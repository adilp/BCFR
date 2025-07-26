import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View, Platform } from 'react-native';
import { useEffect, useState } from 'react';
import { ApiClient } from '@memberorg/api-client';
import { APP_NAME } from '@memberorg/shared';

// Use your computer's IP address for local development
// Or use the deployed API URL for production
const API_URL = Platform.select({
  ios: 'https://memberorg-app-c4a33.ondigitalocean.app/api',     // Production API
  android: 'https://memberorg-app-c4a33.ondigitalocean.app/api', // Production API
  default: 'http://localhost:5001'                               // Web (local)
});

// For local development, uncomment these lines:
// const API_URL = Platform.select({
//   ios: 'http://192.168.3.42:5001',     // Your computer's IP
//   android: 'http://192.168.3.42:5001', // Your computer's IP
//   default: 'http://localhost:5001'     // Web
// });

const apiClient = new ApiClient(API_URL);

export default function App() {
  const [message, setMessage] = useState<string>('Loading...');

  useEffect(() => {
    console.log('Connecting to API at:', API_URL);
    apiClient.getHello()
      .then(data => setMessage(data.message))
      .catch((error) => {
        console.error('API Error:', error);
        setMessage('Failed to connect to API');
      });
  }, []);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{APP_NAME} - Mobile</Text>
      <Text>{message}</Text>
      <Text style={styles.info}>API: {API_URL}</Text>
      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 10,
  },
  info: {
    fontSize: 12,
    color: '#666',
    marginTop: 10,
  },
});