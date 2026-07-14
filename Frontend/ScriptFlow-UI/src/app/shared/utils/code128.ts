// Code128 Subset B encoder. Sufficient for this app's use (prescription SCIDs: uppercase
// letters + digits, all within the ASCII 32-127 range Subset B covers directly) - no need
// for Subset A/C or mode switching.
//
// Pattern table per the published Code128 spec (ISO/IEC 15417): symbol values 0-105 are the
// standard 6-element bar/space width patterns (always starting with a bar, summing to 11
// modules); 106 is the unique 7-element STOP pattern (13 modules).
const PATTERNS: readonly string[] = [
  '212222', '222122', '222221', '121223', '121322', '131222', '122213', '122312', '132212', '221213',
  '221312', '231212', '112232', '122132', '122231', '113222', '123122', '123221', '223211', '221132',
  '221231', '213212', '223112', '312131', '311222', '321122', '321221', '312212', '322112', '322211',
  '212123', '212321', '232121', '111323', '131123', '131321', '112313', '132113', '132311', '211313',
  '231113', '231311', '112133', '112331', '132131', '113123', '113321', '133121', '313121', '211331',
  '231131', '213113', '213311', '213131', '311123', '311321', '331121', '312113', '312311', '332111',
  '314111', '221411', '431111', '111224', '111422', '121124', '121421', '141122', '141221', '112214',
  '112412', '122114', '122411', '142112', '142211', '241211', '221114', '413111', '241112', '134111',
  '111242', '121142', '121241', '114212', '124112', '124211', '411212', '421112', '421211', '212141',
  '214121', '412121', '111143', '111341', '131141', '114113', '114311', '411113', '411311', '113141',
  '114131', '311141', '411131', '211412', '211214', '211232', '2331112',
];

const START_B = 104;
const STOP = 106;

/** Subset B value (0-95) for a printable-ASCII character (space through DEL). */
function subsetBValue(char: string): number {
  const code = char.charCodeAt(0);
  if (code < 32 || code > 127) {
    throw new Error(`Code128 Subset B cannot encode character "${char}" (code ${code}).`);
  }
  return code - 32;
}

/** Encodes `value` as a Code128 Subset B symbol sequence: START_B, one value per character,
 * a mod-103 checksum, then STOP. */
export function encodeCode128(value: string): number[] {
  const charValues = Array.from(value).map(subsetBValue);

  let checksum = START_B;
  charValues.forEach((v, index) => {
    checksum += v * (index + 1);
  });
  checksum %= 103;

  return [START_B, ...charValues, checksum, STOP];
}

/** Module widths (bar, space, bar, space, ... always starting and ending on a bar) for the
 * full barcode representing `value`. */
export function code128ModuleWidths(value: string): number[] {
  return encodeCode128(value).flatMap((symbol) => Array.from(PATTERNS[symbol]).map(Number));
}
