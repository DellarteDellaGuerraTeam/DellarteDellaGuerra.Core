// Offline browser checks only; never invokes the game or alters its assets.
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require(process.env.DADG_PLAYWRIGHT_MODULE || 'playwright');

(async () => {
  const browser = await chromium.launch({ headless: true, channel: 'chrome' });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1400 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(pathToFileURL(path.join(__dirname, 'saveload-preview.html')).href);
    assert.equal(await page.locator('.save').count(), 6);
    assert.equal(await page.locator('.reference').evaluate(img => img.complete && img.naturalWidth > 0), true);
    await page.locator('.save[data-id="1"]').click();
    assert.equal(await page.locator('.save[aria-pressed="true"]').count(), 1);
    await page.locator('.campaign').first().click();
    assert.equal(await page.locator('#group-0').isVisible(), false);
    await page.locator('#reset').click();
    assert.equal(await page.locator('#group-0').isVisible(), true);
    await page.locator('#search').fill('corgie');
    assert.equal(await page.locator('.save:visible').count(), 1);
    await page.locator('#reset').click();
    await page.locator('#unavailable').check();
    assert.equal(await page.locator('.save[data-id="5"]').getAttribute('aria-disabled'), 'true');
    await page.locator('#reset').click();
    await page.locator('#selection').selectOption('1');
    await page.screenshot({ path: path.resolve(__dirname, '../../.codex-work/ui-asset-skin/review/saveload-ledger-preview.png'), fullPage: true });
    await page.setViewportSize({ width: 430, height: 932 });
    await page.locator('#compare').uncheck();
    assert.equal(await page.locator('.reference-column').isVisible(), false);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    assert.deepEqual(errors, []);
    console.log('PASS: images, selection, collapse, search, unavailable sample, reset and mobile overflow.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
