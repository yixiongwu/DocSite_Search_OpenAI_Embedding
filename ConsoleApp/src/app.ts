import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv'

dotenv.config();

function walkDir(dir: string, callback: Function) {
    fs.readdirSync(dir).forEach(f => {
        let dirPath = path.join(dir, f);
        let isDirectory = fs.statSync(dirPath).isDirectory();
        isDirectory ? walkDir(dirPath, callback) : callback(f, path.join(dir, f));
    });
};

class DocItem {
    constructor(title: string, content: string) {
        this.Title = title;
        this.Content = content;
    }
    Title: string;
    Content: string;
}

async function main() {
    let docPath = process.env.DOC_PATH;
    let docItems: Array<DocItem> = new Array<DocItem>();
    walkDir(docPath, (fileName: string, filePath: string) => {
        if (path.extname(fileName).toLowerCase() == ".md") {
            let content = fs.readFileSync(filePath).toString();
            docItems.push(new DocItem(path.basename(fileName), content));
        }
    })
    let jsonStr = JSON.stringify(docItems);
    fs.writeFileSync(".//docItems.json", jsonStr);
}

main()