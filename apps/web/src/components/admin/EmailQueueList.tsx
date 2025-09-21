import { useEffect, useState } from 'react';
import { getApiClient } from '@memberorg/api-client';

type QueueItem = {
  id: string;
  recipientEmail: string;
  recipientName?: string;
  subject: string;
  status: string;
  priority: number;
  scheduledFor?: string;
  retryCount: number;
  updatedAt: string;
};

export default function EmailQueueList() {
  const [items, setItems] = useState<QueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const api = getApiClient();
        const data = await api.getEmailQueue({ take: 100 });
        setItems(data);
      } catch (e: any) {
        setError(e?.message || 'Failed to load email queue');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <div className="placeholder-content">Loading email queue…</div>;
  if (error) return <div className="placeholder-content">{error}</div>;

  return (
    <div>
      <div className="card-header">
        <h3 className="card-title">Email Queue</h3>
      </div>
      <div className="spreadsheet-container">
        <table className="spreadsheet-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Recipient</th>
              <th>Subject</th>
              <th>Status</th>
              <th>Priority</th>
              <th>Scheduled</th>
              <th>Retries</th>
              <th>Updated</th>
            </tr>
          </thead>
          <tbody>
            {items.map((e, idx) => (
              <tr key={e.id}>
                <td>{idx + 1}</td>
                <td>
                  <div>
                    <div style={{ fontWeight: 500 }}>{e.recipientName || '-'}</div>
                    <div style={{ color: '#6C757D', fontSize: '12px' }}>{e.recipientEmail}</div>
                  </div>
                </td>
                <td>{e.subject}</td>
                <td>{e.status}</td>
                <td>{e.priority}</td>
                <td>{e.scheduledFor ? new Date(e.scheduledFor).toLocaleString() : '-'}</td>
                <td>{e.retryCount}</td>
                <td>{new Date(e.updatedAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

