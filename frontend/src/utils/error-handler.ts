/**
 * Extract a meaningful error message from Kiota API errors
 * @param error The error object from a failed API call
 * @param defaultMessage The default message to use if extraction fails
 * @returns A human-readable error message
 */
export function extractErrorMessage(error: unknown, defaultMessage: string = 'An error occurred'): string {
  const err = error as Record<string, unknown> | null | undefined;
  // If it's already a simple string error
  if (typeof error === 'string') {
    return error;
  }

  // Try to extract from various possible error structures
  
  // Check for API response body with message
  if (err?.response) {
    try {
      if (typeof err.response === 'object') {
        // Response is already parsed
        const r = err.response as Record<string, string>;
        if (r.message || r.Message) {
          return r.message || r.Message;
        }
        if (r.title) {
          return r.title;
        }
      } else if (typeof err.response === 'string') {
        // Try to parse response as JSON
        try {
          const parsed = JSON.parse(err.response as string) as Record<string, string>;
          if (parsed.message || parsed.Message) {
            return parsed.message || parsed.Message;
          }
        } catch (_e) {
          // Not JSON, use as-is if it's meaningful
          if (!(err.response as string).includes('unexpected status code')) {
            return err.response as string;
          }
        }
      }
    } catch (_e) {
      // Ignore parse errors
    }
  }

  // Check error body
  if (err?.body) {
    if (typeof err.body === 'object') {
      const b = err.body as Record<string, string>;
      if (b.message || b.Message) {
        return b.message || b.Message;
      }
    } else if (typeof err.body === 'string') {
      try {
        const parsed = JSON.parse(err.body as string) as Record<string, string>;
        if (parsed.message || parsed.Message) {
          return parsed.message || parsed.Message;
        }
      } catch (_e) {
        // Not JSON
        if (!(err.body as string).includes('unexpected status code')) {
          return err.body as string;
        }
      }
    }
  }

  // Check for direct message property
  if (err?.message && !(err.message as string).includes('unexpected status code')) {
    return err.message as string;
  }

  // Check for responseText
  if (err?.responseText) {
    try {
      const parsed = JSON.parse(err.responseText as string) as Record<string, string>;
      if (parsed.message || parsed.Message) {
        return parsed.message || parsed.Message;
      }
    } catch (_e) {
      // Not JSON, use as-is
      if (!(err.responseText as string).includes('unexpected status code')) {
        return err.responseText as string;
      }
    }
  }

  // If we have a status code, provide a better default message
  const status = err?.status || err?.statusCode;
  if (status) {
    switch (status) {
      case 400:
        return 'Bad request - please check your input';
      case 401:
        return 'Unauthorized - please log in';
      case 403:
        return 'Forbidden - you do not have permission';
      case 404:
        return 'Resource not found';
      case 409:
        return 'Conflict - resource already exists';
      case 500:
        return 'Server error - please try again later';
      default:
        return `${defaultMessage} (Status: ${status})`;
    }
  }

  return defaultMessage;
}
