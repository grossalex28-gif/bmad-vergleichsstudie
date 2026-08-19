const euroFormatter = new Intl.NumberFormat('de-DE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function formatEuroAmount(amount: number): string {
  return `${euroFormatter.format(amount)} €`;
}
