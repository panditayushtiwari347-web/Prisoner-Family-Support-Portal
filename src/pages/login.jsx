import { useState } from 'react';
import { useRouter } from 'next/router';
import Head from 'next/head';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const router = useRouter();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      const data = await res.json();
      if (res.ok) {
        localStorage.setItem('token', data.token);
        localStorage.setItem('role', data.role);
        router.push('/dashboard');
      } else {
        setError(data.error || 'Login failed');
      }
    } catch (err) {
      setError('An error occurred during login');
    }
  };

  return (
    <div className="flex h-screen items-center justify-center bg-gray-100">
      <Head><title>Login - Portal</title></Head>
      <form onSubmit={handleLogin} className="p-8 bg-white rounded shadow-md w-full max-w-sm">
        <h2 className="text-2xl font-bold mb-6 text-center">Login to Portal</h2>
        {error && <p className="text-red-500 mb-4 text-center">{error}</p>}
        <input className="w-full p-2 border border-gray-300 rounded mb-4" type="email" placeholder="Email" onChange={e => setEmail(e.target.value)} required />
        <input className="w-full p-2 border border-gray-300 rounded mb-6" type="password" placeholder="Password" onChange={e => setPassword(e.target.value)} required />
        <button className="w-full bg-blue-600 text-white p-2 rounded hover:bg-blue-700 font-semibold" type="submit">Sign In</button>
      </form>
    </div>
  );
}
