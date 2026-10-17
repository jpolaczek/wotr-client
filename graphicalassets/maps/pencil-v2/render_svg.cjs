// Render the native SVG composition, including its grayscale and ink filters.
const fs = require('node:fs');
const { Resvg } = require('@resvg/resvg-js');

const [source, target, width] = process.argv.slice(2);
if (!source || !target) {
  throw new Error('Usage: render_svg.cjs source.svg target.png [width]');
}
const options = width ? { fitTo: { mode: 'width', value: Number(width) } } : {};
const renderer = new Resvg(fs.readFileSync(source), options);
fs.writeFileSync(target, renderer.render().asPng());
