import React from 'react';
import { StudentTable } from '../components/StudentTable';
import { students } from '../data/mockData';

export function DashboardPage() {
  return <div className="flex h-screen w-full bg-slate-50 overflow-hidden font-sans text-slate-900">
      <main className="flex-1 flex flex-col min-w-0 overflow-hidden relative">
        <div className="flex-1 h-full relative">
          <StudentTable students={students} />
        </div>
      </main>
    </div>;
}
