// Custom Bearer Token Authentication Provider for Kiota
import type { AuthenticationProvider, RequestInformation } from '@microsoft/kiota-abstractions';

export class BearerTokenAuthenticationProvider implements AuthenticationProvider {
  private getToken: () => string | null;

  constructor(getTokenFunc?: () => string | null) {
    this.getToken = getTokenFunc || (() => localStorage.getItem('accessToken'));
  }

  authenticateRequest = async (
    request: RequestInformation,
    _additionalAuthenticationContext?: Record<string, unknown>
  ): Promise<void> => {
    const token = this.getToken();
    
    if (token && request.headers) {
      request.headers.add('Authorization', `Bearer ${token}`);
    }
  };
}
