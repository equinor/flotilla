const { execSync } = require('child_process');

let outdated = {};
try {
    execSync('npm outdated --json', { encoding: 'utf8' });
} catch (e) {
    // npm outdated exits with code 1 when packages are outdated; stdout still has the JSON
    outdated = JSON.parse(e.stdout || '{}');
}

const sevenDaysAgo = Date.now() - 7 * 24 * 60 * 60 * 1000;
const toUpdate = [];

for (const [pkg, info] of Object.entries(outdated)) {
    const latest = info.latest;
    try {
        const timeData = JSON.parse(execSync(`npm view ${pkg}@${latest} time --json`, { encoding: 'utf8' }));
        const publishTime = new Date(timeData[latest]).getTime();
        if (publishTime < sevenDaysAgo) {
            toUpdate.push(`${pkg}@${latest}`);
            console.log(`Queuing update: ${pkg}@${latest}`);
        } else {
            console.log(`Skipping ${pkg}@${latest} (released less than 7 days ago)`);
        }
    } catch (e) {
        console.log(`Skipping ${pkg}: ${e.message}`);
    }
}

if (toUpdate.length > 0) {
    execSync(`npm install ${toUpdate.join(' ')}`, { stdio: 'inherit' });
} else {
    console.log('No packages older than 7 days to update');
}
