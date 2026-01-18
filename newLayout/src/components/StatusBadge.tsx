import React from 'react';
import { Status } from '../data/mockData';
interface StatusBadgeProps {
  status: Status;
}
export function StatusBadge({
  status
}: StatusBadgeProps) {
  const styles = {
    Active: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    Pending: 'bg-amber-50 text-amber-700 border-amber-200',
    Completed: 'bg-blue-50 text-blue-700 border-blue-200'
  };
  return <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${styles[status]}`}>
      <span className={`w-1.5 h-1.5 rounded-full mr-1.5 ${status === 'Active' ? 'bg-emerald-500' : status === 'Pending' ? 'bg-amber-500' : 'bg-blue-500'}`} />
      {status}
    </span>;
}