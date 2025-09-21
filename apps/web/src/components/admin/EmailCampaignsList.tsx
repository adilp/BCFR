import { useEffect, useState } from 'react';
import { getApiClient } from '@memberorg/api-client';

type Campaign = {
  id: string;
  name: string;
  type: string;
  status: string;
  totalRecipients: number;
  createdAt: string;
  completedAt?: string;
  stats?: { Sent: number; Failed: number; Pending: number };
};

export default function EmailCampaignsList() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const api = getApiClient();
        const data = await api.getEmailCampaigns();
        setCampaigns(data);
      } catch (e: any) {
        setError(e?.message || 'Failed to load campaigns');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <div className="placeholder-content">Loading campaigns…</div>;
  if (error) return <div className="placeholder-content">{error}</div>;

  return (
    <div>
      <div className="card-header">
        <h3 className="card-title">Email Campaigns</h3>
      </div>
      <div className="spreadsheet-container">
        <table className="spreadsheet-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Name</th>
              <th>Type</th>
              <th>Status</th>
              <th>Sent</th>
              <th>Failed</th>
              <th>Pending</th>
              <th>Total</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            {campaigns.map((c, idx) => (
              <tr key={c.id}>
                <td>{idx + 1}</td>
                <td>{c.name}</td>
                <td><span className="tag tag-purple">{c.type}</span></td>
                <td>{c.status}</td>
                <td>{(c as any).stats?.Sent ?? '-'}</td>
                <td>{(c as any).stats?.Failed ?? '-'}</td>
                <td>{(c as any).stats?.Pending ?? '-'}</td>
                <td>{c.totalRecipients}</td>
                <td>{new Date(c.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

