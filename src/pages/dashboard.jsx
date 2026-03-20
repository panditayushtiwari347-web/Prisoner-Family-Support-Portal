import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import Head from 'next/head';

export default function Dashboard() {
  const [appointments, setAppointments] = useState([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/login');
      return;
    }

    fetch('/api/appointments', {
      headers: { 'Authorization': `Bearer ${token}` }
    })
    .then(res => {
      if (!res.ok) throw new Error('Unauthorized');
      return res.json();
    })
    .then(data => {
      setAppointments(data);
      setLoading(false);
    })
    .catch(() => {
      localStorage.removeItem('token');
      router.push('/login');
    });
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    router.push('/login');
  };

  if (loading) return <div className="p-8 text-center text-gray-500">Loading dashboard...</div>;

  return (
    <div className="min-h-screen bg-gray-50">
      <Head><title>Dashboard</title></Head>
      <nav className="p-4 bg-gray-900 text-white flex justify-between items-center">
        <div className="font-bold text-lg">Prisoner Support Portal</div>
        <button onClick={handleLogout} className="bg-red-500 hover:bg-red-600 px-4 py-2 rounded font-semibold transition">Logout</button>
      </nav>
      <main className="p-8">
        <h1 className="text-3xl font-bold mb-8 text-gray-800">Your Dashboard</h1>
        <div className="bg-white p-6 rounded shadow-md">
          <h2 className="text-xl font-semibold mb-4 text-gray-700">Appointments</h2>
          {appointments.length === 0 ? <p className="text-gray-500">No appointments found.</p> : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-left">
                <thead>
                  <tr className="border-b">
                    <th className="py-2">Date</th>
                    <th className="py-2">Prisoner</th>
                    <th className="py-2">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {appointments.map(app => (
                    <tr key={app.id} className="border-b">
                      <td className="py-2">{new Date(app.date).toLocaleDateString()}</td>
                      <td className="py-2">{app.prisoner?.firstName} {app.prisoner?.lastName}</td>
                      <td className="py-2"><span className="px-2 py-1 bg-blue-100 text-blue-800 rounded text-sm">{app.status}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
