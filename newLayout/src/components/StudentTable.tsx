import React, { useMemo, useState } from 'react';
import { Search, ArrowUpDown, ArrowUp, ArrowDown, Filter, Download } from 'lucide-react';
import { Student, Status } from '../data/mockData';
import { StatusBadge } from './StatusBadge';
interface StudentTableProps {
  students: Student[];
}
type SortField = 'name' | 'company' | 'status' | 'startDate' | 'supervisor';
type SortDirection = 'asc' | 'desc';
export function StudentTable({
  students
}: StudentTableProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<Status | 'All'>('All');
  const [sortField, setSortField] = useState<SortField>('name');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };
  const filteredAndSortedStudents = useMemo(() => {
    return students.filter(student => {
      const matchesSearch = student.name.toLowerCase().includes(searchQuery.toLowerCase()) || student.company.toLowerCase().includes(searchQuery.toLowerCase()) || student.supervisor.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesStatus = statusFilter === 'All' || student.status === statusFilter;
      return matchesSearch && matchesStatus;
    }).sort((a, b) => {
      const aValue = a[sortField];
      const bValue = b[sortField];
      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [students, searchQuery, statusFilter, sortField, sortDirection]);
  const SortIcon = ({
    field
  }: {
    field: SortField;
  }) => {
    if (sortField !== field) return <ArrowUpDown className="w-3 h-3 ml-1 text-slate-400 opacity-0 group-hover:opacity-50" />;
    return sortDirection === 'asc' ? <ArrowUp className="w-3 h-3 ml-1 text-indigo-600" /> : <ArrowDown className="w-3 h-3 ml-1 text-indigo-600" />;
  };
  const Th = ({
    field,
    children,
    className = ''
  }: {
    field: SortField;
    children: React.ReactNode;
    className?: string;
  }) => <th className={`px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer group hover:bg-slate-50 transition-colors select-none sticky top-0 bg-white z-10 border-b border-slate-200 ${className}`} onClick={() => handleSort(field)}>
      <div className="flex items-center">
        {children}
        <SortIcon field={field} />
      </div>
    </th>;
  return <div className="flex-1 flex flex-col h-full overflow-hidden bg-white">
      {/* Header Actions */}
      <div className="px-8 py-6 border-b border-slate-200 flex-shrink-0">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">All Students</h1>
            <p className="text-sm text-slate-500 mt-1">
              Showing {filteredAndSortedStudents.length} of {students.length}{' '}
              students
            </p>
          </div>
          <div className="flex items-center space-x-3">
            <button className="inline-flex items-center px-4 py-2 border border-slate-300 shadow-sm text-sm font-medium rounded-md text-slate-700 bg-white hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
              <Download className="w-4 h-4 mr-2 text-slate-500" />
              Export
            </button>
            <button className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
              Add Student
            </button>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
          <div className="relative w-full sm:w-72">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <Search className="h-4 w-4 text-slate-400" />
            </div>
            <input type="text" className="block w-full pl-10 pr-3 py-2 border border-slate-300 rounded-md leading-5 bg-white placeholder-slate-400 focus:outline-none focus:placeholder-slate-300 focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm transition duration-150 ease-in-out" placeholder="Search students, companies..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
          </div>

          <div className="flex items-center space-x-2 overflow-x-auto pb-2 sm:pb-0 max-w-full">
            <Filter className="w-4 h-4 text-slate-400 mr-2 flex-shrink-0" />
            {(['All', 'Active', 'Pending', 'Completed'] as const).map(status => <button key={status} onClick={() => setStatusFilter(status)} className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors whitespace-nowrap ${statusFilter === status ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
                  {status}
                </button>)}
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead>
            <tr>
              <Th field="name">Student Name</Th>
              <Th field="company">Placement</Th>
              <Th field="status">Status</Th>
              <Th field="startDate">Start Date</Th>
              <Th field="supervisor">Supervisor</Th>
              <th className="px-6 py-3 bg-white border-b border-slate-200 sticky top-0 z-10"></th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-slate-200">
            {filteredAndSortedStudents.length > 0 ? filteredAndSortedStudents.map(student => <tr key={student.id} className="hover:bg-slate-50 transition-colors group cursor-pointer">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="h-8 w-8 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-700 font-bold text-xs mr-3">
                        {student.name.split(' ').map(n => n[0]).join('')}
                      </div>
                      <div>
                        <div className="text-sm font-medium text-slate-900">
                          {student.name}
                        </div>
                        <div className="text-xs text-slate-500">
                          {student.email}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-slate-900 font-medium">
                      {student.company}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <StatusBadge status={student.status} />
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-500 font-mono">
                    {new Date(student.startDate).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric'
              })}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-600">
                    {student.supervisor}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button className="text-indigo-600 hover:text-indigo-900 opacity-0 group-hover:opacity-100 transition-opacity">
                      Edit
                    </button>
                  </td>
                </tr>) : <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-slate-500">
                  <div className="flex flex-col items-center justify-center">
                    <Search className="w-8 h-8 text-slate-300 mb-3" />
                    <p className="text-sm font-medium text-slate-900">
                      No students found
                    </p>
                    <p className="text-sm text-slate-500 mt-1">
                      Try adjusting your search or filters
                    </p>
                  </div>
                </td>
              </tr>}
          </tbody>
        </table>
      </div>
    </div>;
}