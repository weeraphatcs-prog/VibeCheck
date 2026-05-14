const puppeteer = require('puppeteer-core');

(async () => {
  const browser = await puppeteer.launch({
    executablePath: '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1280, height: 720 });
  await page.goto('http://localhost:8765/slides.html?print-pdf', {
    waitUntil: 'networkidle0',
    timeout: 30000,
  });
  await new Promise(r => setTimeout(r, 3000));

  await page.pdf({
    path: '/mnt/c/Users/iLink_gram/Documents/project/KahootClone/VibeCheck-slides.pdf',
    width: '1280px',
    height: '720px',
    printBackground: true,
    margin: { top: '0', right: '0', bottom: '0', left: '0' },
  });

  await browser.close();
  console.log('Done');
})();
