import { PrismaClient } from '@prisma/client';
import jwt from 'jsonwebtoken';

const globalForPrisma = global;
const prisma = globalForPrisma.prisma || new PrismaClient();
if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = prisma;

export default async function handler(req, res) {
  const authHeader = req.headers.authorization;
  if (!authHeader) return res.status(401).json({ error: 'Unauthorized' });
  
  const token = authHeader.split(' ')[1];
  let user;
  try {
    user = jwt.verify(token, process.env.JWT_SECRET || 'fallback_secret_do_not_use');
  } catch (e) {
    return res.status(401).json({ error: 'Invalid token' });
  }

  if (req.method === 'GET') {
    try {
      const appointments = user.role === 'Admin' 
        ? await prisma.appointment.findMany({ include: { family: true, prisoner: true } })
        : await prisma.appointment.findMany({ where: { familyId: user.id }, include: { prisoner: true } });
      return res.status(200).json(appointments);
    } catch (err) {
      console.error(err);
      return res.status(500).json({ error: 'Database error' });
    }
  }

  res.status(405).json({ error: 'Method not allowed' });
}
