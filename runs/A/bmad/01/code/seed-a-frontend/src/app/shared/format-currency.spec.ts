import { formatEuroAmount } from './format-currency';

describe('formatEuroAmount', () => {
  it('formats a whole euro amount with two decimal places and a comma', () => {
    expect(formatEuroAmount(32)).toBe('32,00 €');
  });

  it('formats a fractional euro amount with a comma decimal separator', () => {
    expect(formatEuroAmount(22.5)).toBe('22,50 €');
  });

  it('formats a four-digit euro amount with a thousands separator', () => {
    expect(formatEuroAmount(1234.5)).toBe('1.234,50 €');
  });
});
