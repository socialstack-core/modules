const { execSync } = require('child_process');
const path = require('path');

const projectRoot = path.join(__dirname, '..');

console.log('Running TypeScript check...\n');

let output;
try {
    output = execSync('npx tsc --noEmit -p tsconfig.json', {
        cwd: projectRoot,
        stdio: 'pipe',
        encoding: 'utf-8'
    });
    console.log('No TypeScript errors found!');
} catch (e) {
    output = e.stdout ? e.stdout.toString() : e.message;
    
    const errorLines = output.split('\n').filter(l => l.match(/error TS\d+/));
    
    const filesMap = {};
    for (let i = 0; i < errorLines.length; i++) {
        const line = errorLines[i];
        const match = line.match(/^(.+?)\(\d+,\d+\):/);
        if (match) {
            const filePath = match[1];
            if (!filesMap[filePath]) {
                filesMap[filePath] = [];
            }
            filesMap[filePath].push(line);
        }
    }
    
    const fileCount = Object.keys(filesMap).length;
    console.log('Found TypeScript errors in ' + fileCount + ' files:\n');
    
    for (const filePath in filesMap) {
        console.log('\n' + filePath + ':');
        for (let i = 0; i < filesMap[filePath].length; i++) {
            console.log('  ' + filesMap[filePath][i]);
        }
    }
}