export class ToolNotFoundError extends Error {
  constructor(id: string) {
    super(`Werkzeug mit id "${id}" wurde nicht gefunden.`);
    this.name = 'ToolNotFoundError';
  }
}

export class ToolAlreadyBorrowedError extends Error {
  constructor(id: string) {
    super(`Werkzeug mit id "${id}" ist bereits ausgeliehen.`);
    this.name = 'ToolAlreadyBorrowedError';
  }
}

export class ToolNotBorrowedError extends Error {
  constructor(id: string) {
    super(`Werkzeug mit id "${id}" ist nicht ausgeliehen.`);
    this.name = 'ToolNotBorrowedError';
  }
}

export class ValidationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ValidationError';
  }
}
