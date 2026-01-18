import React from 'react';
import { Calendar, User, Building2 } from 'lucide-react';
import { Student } from '../data/mockData';
import { StatusBadge } from './StatusBadge';
interface StudentCardProps {
  student: Student;
}
export function StudentCard({
  student
}: StudentCardProps) {
  return <div className="group bg-white rounded-lg border border-slate-200 p-4 hover:border-indigo-300 hover:shadow-md transition-all duration-200 cursor-pointer">
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-600 font-bold text-sm ring-2 ring-white">
            {student.name.split(' ').map(n => n[0]).join('')}
          </div>
          <div>
            <h3 className="text-sm font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">
              {student.name}
            </h3>
            <p className="text-xs text-slate-500">{student.email}</p>
          </div>
        </div>
        <StatusBadge status={student.status} />
      </div>

      <div className="space-y-2 pt-2 border-t border-slate-50">
        <div className="flex items-center text-xs text-slate-600">
          <Building2 className="w-3.5 h-3.5 mr-2 text-slate-400" />
          <span className="font-medium">{student.company}</span>
        </div>

        <div className="flex items-center text-xs text-slate-600">
          <User className="w-3.5 h-3.5 mr-2 text-slate-400" />
          <span>Sup: {student.supervisor}</span>
        </div>

        <div className="flex items-center text-xs text-slate-500">
          <Calendar className="w-3.5 h-3.5 mr-2 text-slate-400" />
          <span>
            Started{' '}
            {new Date(student.startDate).toLocaleDateString(undefined, {
            month: 'short',
            day: 'numeric',
            year: 'numeric'
          })}
          </span>
        </div>
      </div>
    </div>;
}