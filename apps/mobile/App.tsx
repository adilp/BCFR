import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View } from 'react-native';
import { useEffect, useState } from 'react';
import { ApiClient } from '@memberorg/api-client';
import { APP_NAME } from '@memberorg/shared';

const apiClient = new ApiClient('http://localhost:5000');

export default function App() {
  const [message, setMessage] = useState<string>('Loading...');

  useEffect(() => {
    apiClient.getHello()
      .then(data => setMessage(data.message))
      .catch(() => setMessage('Failed to connect to API'));
  }, []);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{APP_NAME} - Mobile</Text>
      <Text>{message}</Text>
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
});