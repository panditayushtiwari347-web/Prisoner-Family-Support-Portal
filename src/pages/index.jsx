import Link from 'next/link';
import Head from 'next/head';

export default function Home() {
  return (
    <div className="min-h-screen bg-gray-100 flex flex-col items-center justify-center p-4">
      <Head>
        <title>Prisoner Family Support Portal</title>
      </Head>
      <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-8">
        <h1 className="text-2xl font-bold text-center mb-6 text-gray-800">Prisoner Family Support Portal</h1>
        <p className="text-gray-600 mb-8 text-center">Welcome. Please login or register to manage support requests and appointments.</p>
        <div className="flex flex-col gap-4">
          <Link href="/login" className="w-full text-center bg-blue-600 text-white py-2 rounded font-semibold hover:bg-blue-700 transition">Login</Link>
          <Link href="/register" className="w-full text-center bg-gray-200 text-gray-800 py-2 rounded font-semibold hover:bg-gray-300 transition">Register</Link>
        </div>
      </div>
    </div>
  );
}
