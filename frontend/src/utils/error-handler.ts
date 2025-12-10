/**
 * Extract a meaningful error message from Kiota API errors
 * @param error The error object from a failed API call
 * @param defaultMessage The default message to use if extraction fails
 * @returns A human-readable error message
 */
export function extractErrorMessage(error: any, defaultMessage: string = 'An error occurred'): string {
  // If it's already a simple string error
  if (typeof error === 'string') {
    return error;
  }

  // Try to extract from various possible error structures
  
  // Check for API response body with message
  if (error?.response) {
    try {
      if (typeof error.response === 'object') {
        // Response is already parsed
        if (error.response.message || error.response.Message) {
          return error.response.message || error.response.Message;
        }
        if (error.response.title) {
          return error.response.title;
        }
      } else if (typeof error.response === 'string') {
        // Try to parse response as JSON
        try {
          const parsed = JSON.parse(error.response);
          if (parsed.message || parsed.Message) {
            return parsed.message || parsed.Message;
          }
        } catch (e) {
          // Not JSON, use as-is if it's meaningful
          if (!error.response.includes('unexpected status code')) {
            return error.response;
          }
        }
      }
    } catch (e) {
      // Ignore parse errors
    }
  }

  // Check error body
  if (error?.body) {
    if (typeof error.body === 'object') {
      if (error.body.message || error.body.Message) {
        return error.body.message || error.body.Message;
      }
    } else if (typeof error.body === 'string') {
      try {
        const parsed = JSON.parse(error.body);
        if (parsed.message || parsed.Message) {
          return parsed.message || parsed.Message;
        }
      } catch (e) {
        // Not JSON
        if (!error.body.includes('unexpected status code')) {
          return error.body;
        }
      }
    }
  }

  // Check for direct message property
  if (error?.message && !error.message.includes('unexpected status code')) {
    return error.message;
  }

  // Check for responseText
  if (error?.responseText) {
    try {
      const parsed = JSON.parse(error.responseText);
      if (parsed.message || parsed.Message) {
        return parsed.message || parsed.Message;
      }
    } catch (e) {
      // Not JSON, use as-is
      if (!error.responseText.includes('unexpected status code')) {
        return error.responseText;
      }
    }
  }

  // If we have a status code, provide a better default message
  const status = error?.status || error?.statusCode;
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
