import * as signalR from '@microsoft/signalr';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Map<string, Set<Function>> = new Map();

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const token = localStorage.getItem('accessToken');
    const url = `${import.meta.env.VITE_API_GATEWAY_URL}/dashboardHub`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => token || '',
        skipNegotiation: false,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Setup event handlers
    this.connection.on('ReceiveDashboardUpdate', (data) => {
      this.notifyListeners('dashboardUpdate', data);
    });

    this.connection.on('ReceiveAlert', (data) => {
      this.notifyListeners('alert', data);
    });

    this.connection.on('ReceiveKPIUpdate', (data) => {
      this.notifyListeners('kpiUpdate', data);
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
    });

    try {
      await this.connection.start();
      console.log('SignalR connected');
      await this.subscribeToMetrics();
    } catch (error) {
      console.error('SignalR connection error:', error);
      throw error;
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.listeners.clear();
    }
  }

  async subscribeToMetrics() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('SubscribeToMetrics');
      } catch (error) {
        console.error('Error subscribing to metrics:', error);
      }
    }
  }

  async unsubscribeFromMetrics() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('UnsubscribeFromMetrics');
      } catch (error) {
        console.error('Error unsubscribing from metrics:', error);
      }
    }
  }

  on(event: string, callback: Function) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(callback);

    return () => this.off(event, callback);
  }

  off(event: string, callback: Function) {
    const eventListeners = this.listeners.get(event);
    if (eventListeners) {
      eventListeners.delete(callback);
    }
  }

  private notifyListeners(event: string, data: any) {
    const eventListeners = this.listeners.get(event);
    if (eventListeners) {
      eventListeners.forEach((callback) => callback(data));
    }
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();
export default signalRService;
