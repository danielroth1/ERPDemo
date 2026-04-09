/**
 * Safely extract error message from unknown error types
 * Handles Error objects, plain objects with message property, and strings
 */
export function getErrorMessage(error: unknown, defaultMessage: string = 'Unknown error occurred'): string {
  if (error instanceof Error) {
    return error.message;
  }
  
  if (typeof error === 'object' && error !== null && 'message' in error) {
    const msg = (error as Record<string, unknown>).message;
    if (typeof msg === 'string') {
      return msg;
    }
  }
  
  if (typeof error === 'string') {
    return error;
  }
  
  return defaultMessage;
}

/**
 * Safely extract data property from response objects
 * Used for Kiota-generated API client responses that may have data property
 */
export function getResponseData<T>(response: unknown): T | undefined {
  if (typeof response === 'object' && response !== null && 'data' in response) {
    return (response as Record<string, unknown>).data as T;
  }
  return undefined;
}

/**
 * Type guard for Error objects
 */
export function isError(value: unknown): value is Error {
  return value instanceof Error;
}
