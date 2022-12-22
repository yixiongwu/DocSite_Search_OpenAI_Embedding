import { promisify } from "util";
import path, { resolve } from "path";
import fs from "fs";

import dotenv from "dotenv";
import { read } from "to-vfile";
import { unified } from "unified";
import { visit } from "unist-util-visit";
import remarkParse from "remark-parse";
import remarkStringify from "remark-stringify";

const readdir = promisify(fs.readdir);
const stat = promisify(fs.stat);

dotenv.config();

async function getFiles(dir) {
  const subdirs = await readdir(dir);
  const files = await Promise.all(
    subdirs.map(async (subdir) => {
      const res = resolve(dir, subdir);
      return (await stat(res)).isDirectory() ? getFiles(res) : res;
    })
  );
  return files.reduce((a, f) => a.concat(f), []);
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
  const type = "text";
  let title = "";
  let content = "";
  await visitAsync(AST, null, async (node) => {
    if (node.type === type && node.value) {
      let text = node.value;
      if (text.startsWith("title:")) {
        title = text.substr(5);
      } else {
        content += text;
      }
    }
  });
  /*
  'root',
  'thematicBreak',
  'heading',
  'paragraph',
  'inlineCode',
  'html',
  'text',
  'strong',
  'list',
  'listItem',
  'image',
  'code'
  */
  return [title, content];
}

class DocItem {
  constructor(fileName, title, content) {
    this.FileName = fileName;
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
  let filePaths = await getFiles(docPath);

  await Promise.all(
    filePaths.map(async (filePath) => {
      if (path.extname(filePath).toLowerCase() == ".md") {
        const file = await read(filePath);
        const result = processor.parse(file);
        let AST = await processor.run(result);
        let [title, content] = await visitAndExtractContent(AST);
        let fileName = filePath.substr(process.env.DOC_PATH.length);
        docItems.push(new DocItem(fileName, title, content));
      }
    })
  );

  let jsonStr = JSON.stringify(docItems);
  fs.writeFileSync(".//docItems.json", jsonStr);
}

main();
