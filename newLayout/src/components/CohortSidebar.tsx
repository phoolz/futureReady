import React from 'react';
import { Users, BookOpen, ChevronRight } from 'lucide-react';
import { Cohort } from '../data/mockData';
interface CohortSidebarProps {
  cohorts: Cohort[];
  selectedCohortId: string;
  onSelectCohort: (id: string) => void;
}
export function CohortSidebar({
  cohorts,
  selectedCohortId,
  onSelectCohort
}: CohortSidebarProps) {
  return <aside className="w-64 bg-white border-r border-slate-200 flex flex-col h-full flex-shrink-0">
      <div className="p-6 border-b border-slate-100">
        <div className="flex items-center space-x-2 text-slate-900 mb-1">
          <div className="bg-indigo-600 p-1.5 rounded-md">
            <BookOpen className="w-4 h-4 text-white" />
          </div>
          <span className="font-semibold text-lg tracking-tight">EduTrack</span>
        </div>
        <p className="text-xs text-slate-500 ml-9">Placement Management</p>
      </div>

      <div className="p-4">
        <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3 px-2">
          Cohorts
        </h3>
        <nav className="space-y-1">
          {cohorts.map(cohort => {
          const isSelected = selectedCohortId === cohort.id;
          return <button key={cohort.id} onClick={() => onSelectCohort(cohort.id)} className={`w-full flex items-center justify-between px-3 py-2.5 text-sm font-medium rounded-md transition-colors duration-150 ${isSelected ? 'bg-indigo-50 text-indigo-700' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'}`}>
                <div className="flex items-center">
                  <Users className={`w-4 h-4 mr-3 ${isSelected ? 'text-indigo-500' : 'text-slate-400'}`} />
                  {cohort.name}
                </div>
                <span className={`text-xs py-0.5 px-2 rounded-full ${isSelected ? 'bg-indigo-100 text-indigo-700' : 'bg-slate-100 text-slate-500'}`}>
                  {cohort.studentCount}
                </span>
              </button>;
        })}
        </nav>
      </div>

      <div className="mt-auto p-4 border-t border-slate-100">
        <button className="flex items-center w-full px-3 py-2 text-sm font-medium text-slate-600 rounded-md hover:bg-slate-50 transition-colors">
          <div className="w-8 h-8 rounded-full bg-slate-200 flex items-center justify-center mr-3 text-slate-500">
            JD
          </div>
          <div className="flex-1 text-left">
            <p className="text-slate-900">Jane Doe</p>
            <p className="text-xs text-slate-500">Program Director</p>
          </div>
          <ChevronRight className="w-4 h-4 text-slate-400" />
        </button>
      </div>
    </aside>;
}