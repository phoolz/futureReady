import React, { useState } from 'react';
import { LayoutGrid, List } from 'lucide-react';
import { CohortSidebar } from '../components/CohortSidebar';
import { StudentTable } from '../components/StudentTable';
import { CohortGroupView } from '../components/CohortGroupView';
import { cohorts, students } from '../data/mockData';
import { motion } from 'framer-motion';
type ViewMode = 'table' | 'grouped';
export function DashboardPage() {
  const [selectedCohortId, setSelectedCohortId] = useState(cohorts[0].id);
  const [viewMode, setViewMode] = useState<ViewMode>('table');
  const selectedCohort = cohorts.find(c => c.id === selectedCohortId) || cohorts[0];
  // For table view, we filter by selected cohort
  const cohortStudents = students.filter(s => s.cohortId === selectedCohortId);
  return <div className="flex h-screen w-full bg-slate-50 overflow-hidden font-sans text-slate-900">
      {/* Sidebar only shows in table mode or if we want it persistent.
            The requirement implies a toggle between views.
            If we are in 'Group by Cohort' mode, the sidebar might be redundant
            since we show all cohorts, but let's keep it for navigation consistency
            or hide it based on design preference.
            Given the prompt says "Update the dashboard to show students grouped by cohort... with a toggle",
            I'll keep the sidebar for Table view but maybe hide it or disable it for Grouped view
            to give more space, OR keep it but make it act as a filter.
            
            Actually, "Group by Cohort" usually implies seeing ALL cohorts at once,
            whereas the sidebar selects ONE cohort.
            Let's hide sidebar in Grouped mode to maximize space and reduce confusion.
        */}

      {viewMode === 'table' && <CohortSidebar cohorts={cohorts} selectedCohortId={selectedCohortId} onSelectCohort={setSelectedCohortId} />}

      <main className="flex-1 flex flex-col min-w-0 overflow-hidden relative">
        {/* View Toggle - Floating or Top Bar */}
        <div className="absolute top-6 right-8 z-20 flex items-center bg-white rounded-lg p-1 shadow-sm border border-slate-200">
          <button onClick={() => setViewMode('table')} className={`relative px-3 py-1.5 text-sm font-medium rounded-md transition-colors flex items-center gap-2 ${viewMode === 'table' ? 'text-indigo-600' : 'text-slate-600 hover:text-slate-900'}`}>
            {viewMode === 'table' && <motion.div layoutId="viewToggle" className="absolute inset-0 bg-indigo-50 rounded-md border border-indigo-100" transition={{
            type: 'spring',
            bounce: 0.2,
            duration: 0.6
          }} />}
            <span className="relative z-10 flex items-center gap-2">
              <List className="w-4 h-4" />
              Table View
            </span>
          </button>

          <button onClick={() => setViewMode('grouped')} className={`relative px-3 py-1.5 text-sm font-medium rounded-md transition-colors flex items-center gap-2 ${viewMode === 'grouped' ? 'text-indigo-600' : 'text-slate-600 hover:text-slate-900'}`}>
            {viewMode === 'grouped' && <motion.div layoutId="viewToggle" className="absolute inset-0 bg-indigo-50 rounded-md border border-indigo-100" transition={{
            type: 'spring',
            bounce: 0.2,
            duration: 0.6
          }} />}
            <span className="relative z-10 flex items-center gap-2">
              <LayoutGrid className="w-4 h-4" />
              Group by Cohort
            </span>
          </button>
        </div>

        {/* Content Area */}
        <div className="flex-1 h-full relative">
          {viewMode === 'table' ? <StudentTable students={cohortStudents} cohortName={selectedCohort.name} /> : <CohortGroupView cohorts={cohorts} students={students} />}
        </div>
      </main>
    </div>;
}