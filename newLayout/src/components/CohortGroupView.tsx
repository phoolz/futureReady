import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, Users, Search, Download, Plus } from 'lucide-react';
import { Cohort, Student } from '../data/mockData';
import { StudentCard } from './StudentCard';
interface CohortGroupViewProps {
  cohorts: Cohort[];
  students: Student[];
}
export function CohortGroupView({
  cohorts,
  students
}: CohortGroupViewProps) {
  const [searchQuery, setSearchQuery] = useState('');
  // Initialize with all cohorts expanded
  const [expandedCohorts, setExpandedCohorts] = useState<Set<string>>(new Set(cohorts.map(c => c.id)));
  const toggleCohort = (cohortId: string) => {
    const newExpanded = new Set(expandedCohorts);
    if (newExpanded.has(cohortId)) {
      newExpanded.delete(cohortId);
    } else {
      newExpanded.add(cohortId);
    }
    setExpandedCohorts(newExpanded);
  };
  // Filter students based on search
  const getFilteredStudents = (cohortId: string) => {
    return students.filter(s => s.cohortId === cohortId && (s.name.toLowerCase().includes(searchQuery.toLowerCase()) || s.company.toLowerCase().includes(searchQuery.toLowerCase())));
  };
  return <div className="flex-1 flex flex-col h-full overflow-hidden bg-slate-50">
      {/* Header Actions */}
      <div className="px-8 py-6 bg-white border-b border-slate-200 flex-shrink-0">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">All Cohorts</h1>
            <p className="text-sm text-slate-500 mt-1">
              Overview of student placements across all cohorts
            </p>
          </div>
          <div className="flex items-center space-x-3">
            <button className="inline-flex items-center px-4 py-2 border border-slate-300 shadow-sm text-sm font-medium rounded-md text-slate-700 bg-white hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors">
              <Download className="w-4 h-4 mr-2 text-slate-500" />
              Export Report
            </button>
            <button className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors">
              <Plus className="w-4 h-4 mr-2" />
              Add Student
            </button>
          </div>
        </div>

        <div className="relative max-w-md">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <Search className="h-4 w-4 text-slate-400" />
          </div>
          <input type="text" className="block w-full pl-10 pr-3 py-2 border border-slate-300 rounded-md leading-5 bg-white placeholder-slate-400 focus:outline-none focus:placeholder-slate-300 focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm transition duration-150 ease-in-out" placeholder="Search across all cohorts..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
        </div>
      </div>

      {/* Scrollable Content */}
      <div className="flex-1 overflow-y-auto px-8 py-6 space-y-6">
        {cohorts.map(cohort => {
        const cohortStudents = getFilteredStudents(cohort.id);
        const isExpanded = expandedCohorts.has(cohort.id);
        // Skip rendering cohort if searching and no matches
        if (searchQuery && cohortStudents.length === 0) return null;
        return <div key={cohort.id} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
              <button onClick={() => toggleCohort(cohort.id)} className="w-full flex items-center justify-between px-6 py-4 bg-white hover:bg-slate-50 transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-indigo-500">
                <div className="flex items-center gap-3">
                  <div className={`p-1.5 rounded-md transition-colors ${isExpanded ? 'bg-indigo-100 text-indigo-600' : 'bg-slate-100 text-slate-500'}`}>
                    <motion.div animate={{
                  rotate: isExpanded ? 90 : 0
                }} transition={{
                  duration: 0.2
                }}>
                      <ChevronRight className="w-4 h-4" />
                    </motion.div>
                  </div>
                  <div className="text-left">
                    <h2 className="text-lg font-semibold text-slate-900">
                      {cohort.name}
                    </h2>
                    <div className="flex items-center text-xs text-slate-500 mt-0.5">
                      <Users className="w-3 h-3 mr-1.5" />
                      {cohortStudents.length} students
                    </div>
                  </div>
                </div>

                {/* Progress bar mini visualization could go here */}
                <div className="hidden sm:flex items-center gap-2">
                  <div className="text-xs font-medium text-slate-500">
                    {cohortStudents.filter(s => s.status === 'Completed').length}{' '}
                    / {cohortStudents.length} Completed
                  </div>
                  <div className="w-24 h-1.5 bg-slate-100 rounded-full overflow-hidden">
                    <div className="h-full bg-green-500 rounded-full" style={{
                  width: `${cohortStudents.filter(s => s.status === 'Completed').length / Math.max(cohortStudents.length, 1) * 100}%`
                }} />
                  </div>
                </div>
              </button>

              <AnimatePresence initial={false}>
                {isExpanded && <motion.div initial={{
              height: 0,
              opacity: 0
            }} animate={{
              height: 'auto',
              opacity: 1
            }} exit={{
              height: 0,
              opacity: 0
            }} transition={{
              duration: 0.3,
              ease: 'easeInOut'
            }}>
                    <div className="px-6 pb-6 pt-2 border-t border-slate-100">
                      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                        {cohortStudents.map((student, index) => <motion.div key={student.id} initial={{
                    opacity: 0,
                    y: 10
                  }} animate={{
                    opacity: 1,
                    y: 0
                  }} transition={{
                    delay: index * 0.05,
                    duration: 0.2
                  }}>
                            <StudentCard student={student} />
                          </motion.div>)}
                      </div>
                      {cohortStudents.length === 0 && <div className="text-center py-8 text-slate-500 text-sm">
                          No students found in this cohort matching your search.
                        </div>}
                    </div>
                  </motion.div>}
              </AnimatePresence>
            </div>;
      })}

        {cohorts.length === 0 && <div className="text-center py-12">
            <p className="text-slate-500">No cohorts found.</p>
          </div>}
      </div>
    </div>;
}