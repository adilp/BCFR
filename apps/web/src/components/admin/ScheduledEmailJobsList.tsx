import { useEffect, useState } from 'react';
import { getApiClient } from '@memberorg/api-client';

type Job = {
  id: string;
  jobType: string;
  entityType: string;
  entityId: string;
  scheduledFor: string;
  nextRunDate?: string;
  lastRunDate?: string;
  status: string;
  runCount: number;
  failureCount: number;
  createdAt: string;
};

export default function ScheduledEmailJobsList() {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const api = getApiClient();
        const data = await api.getScheduledEmailJobs({ status: 'Active', take: 100 });
        setJobs(data);
      } catch (e: any) {
        setError(e?.message || 'Failed to load scheduled jobs');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <div className="placeholder-content">Loading scheduled email jobs…</div>;
  if (error) return <div className="placeholder-content">{error}</div>;

  return (
    <div>
      <div className="card-header">
        <h3 className="card-title">Scheduled Email Jobs</h3>
      </div>
      <div className="spreadsheet-container">
        <table className="spreadsheet-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Job Type</th>
              <th>Entity</th>
              <th>Scheduled For</th>
              <th>Next Run</th>
              <th>Last Run</th>
              <th>Status</th>
              <th>Runs</th>
              <th>Failures</th>
            </tr>
          </thead>
          <tbody>
            {jobs.map((j, idx) => (
              <tr key={j.id}>
                <td>{idx + 1}</td>
                <td><span className="tag tag-blue">{j.jobType}</span></td>
                <td>{j.entityType} · {j.entityId}</td>
                <td>{new Date(j.scheduledFor).toLocaleString()}</td>
                <td>{j.nextRunDate ? new Date(j.nextRunDate).toLocaleString() : '-'}</td>
                <td>{j.lastRunDate ? new Date(j.lastRunDate).toLocaleString() : '-'}</td>
                <td>{j.status}</td>
                <td>{j.runCount}</td>
                <td>{j.failureCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

