import fs from "fs";
import path from "path";
import { read } from "to-vfile";
import { unified } from "unified";
import dotenv from "dotenv";
import { visit, Test, Visitor } from "unist-util-visit";
import remarkParse from "remark-parse";
import remarkStringify from "remark-stringify";

dotenv.config();

function walkDir(dir, callback) {
  fs.readdirSync(dir).forEach((f) => {
    let dirPath = path.join(dir, f);
    let isDirectory = fs.statSync(dirPath).isDirectory();
    isDirectory ? walkDir(dirPath, callback) : callback(f, path.join(dir, f));
  });
}

async function visitAsync(tree, matcher, asyncVisitor) {
  const matches = [];
  visit(tree, matcher, (...args) => {
    matches.push(args);
    return tree;
  });

  const promises = matches.map((match) => asyncVisitor(...match));
  await Promise.all(promises);

  return tree;
}

async function visitAndExtractContent(AST) {
  let texts = [];
  await visitAsync(AST, null, async (node) => {
    if (
      (node.type === "text" || node.type === "html") &&
      node.value != "\n" &&
      node.value.length > 1
    ) {
      // translateParam.Text = node.value;
      // let translatedText = await translateAsync(translateParam);
      // node.value = translatedText;
      texts.push(node.value);
    }
  });
  return texts.join("\n");
}

class DocItem {
  constructor(title, content) {
    this.Title = title;
    this.Content = content;
  }
}

async function main() {
  let docPath =
    process.env.DOC_PATH ?? "./creator-docs-internal/content/en-us/docs";
  if (!fs.existsSync(docPath)) {
    console.error(`${docPath} is not exist.`);
    return;
  }
  const processor = unified().use(remarkParse).use(remarkStringify);

  let docItems = [];
  walkDir(docPath, async (fileName, filePath) => {
    if (path.extname(fileName).toLowerCase() == ".md") {
      const file = await read(filePath);
      const result = processor.parse(file);
      let AST = await processor.run(result);
      let content = await visitAndExtractContent(AST);
      docItems.push(new DocItem(path.basename(fileName), content));
    }
  });
  let jsonStr = JSON.stringify(docItems);
  fs.writeFileSync(".//docItems.json", jsonStr);
}

main();
