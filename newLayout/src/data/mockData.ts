export type Status = 'Active' | 'Pending' | 'Completed';
export interface Student {
  id: string;
  name: string;
  company: string;
  status: Status;
  startDate: string;
  supervisor: string;
  cohortId: string;
  email: string;
}
export interface Cohort {
  id: string;
  name: string;
  studentCount: number;
}
export const cohorts: Cohort[] = [{
  id: 'c1',
  name: 'Fall 2024',
  studentCount: 8
}, {
  id: 'c2',
  name: 'Spring 2024',
  studentCount: 6
}, {
  id: 'c3',
  name: 'Fall 2023',
  studentCount: 5
}];
export const students: Student[] = [
// Fall 2024
{
  id: 's1',
  name: 'Alex Thompson',
  company: 'TechFlow Inc.',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Sarah Chen',
  cohortId: 'c1',
  email: 'alex.t@example.com'
}, {
  id: 's2',
  name: 'Jordan Lee',
  company: 'DataSystems',
  status: 'Pending',
  startDate: '2024-09-15',
  supervisor: 'Mike Ross',
  cohortId: 'c1',
  email: 'jordan.l@example.com'
}, {
  id: 's3',
  name: 'Casey Smith',
  company: 'Innovate Corp',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Jessica Wu',
  cohortId: 'c1',
  email: 'casey.s@example.com'
}, {
  id: 's4',
  name: 'Taylor Brown',
  company: 'Design Studio',
  status: 'Active',
  startDate: '2024-09-10',
  supervisor: 'David Kim',
  cohortId: 'c1',
  email: 'taylor.b@example.com'
}, {
  id: 's5',
  name: 'Morgan Wilson',
  company: 'Cloud Networks',
  status: 'Pending',
  startDate: '2024-10-01',
  supervisor: 'Emily Davis',
  cohortId: 'c1',
  email: 'morgan.w@example.com'
}, {
  id: 's6',
  name: 'Riley Garcia',
  company: 'FinTech Solutions',
  status: 'Active',
  startDate: '2024-09-05',
  supervisor: 'Robert Johnson',
  cohortId: 'c1',
  email: 'riley.g@example.com'
}, {
  id: 's7',
  name: 'Quinn Martinez',
  company: 'HealthPlus',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Lisa Anderson',
  cohortId: 'c1',
  email: 'quinn.m@example.com'
}, {
  id: 's8',
  name: 'Avery Robinson',
  company: 'EcoEnergy',
  status: 'Pending',
  startDate: '2024-09-20',
  supervisor: 'Tom White',
  cohortId: 'c1',
  email: 'avery.r@example.com'
},
// Spring 2024
{
  id: 's9',
  name: 'Jamie Clark',
  company: 'TechFlow Inc.',
  status: 'Completed',
  startDate: '2024-01-15',
  supervisor: 'Sarah Chen',
  cohortId: 'c2',
  email: 'jamie.c@example.com'
}, {
  id: 's10',
  name: 'Drew Rodriguez',
  company: 'DataSystems',
  status: 'Completed',
  startDate: '2024-01-20',
  supervisor: 'Mike Ross',
  cohortId: 'c2',
  email: 'drew.r@example.com'
}, {
  id: 's11',
  name: 'Sam Patels',
  company: 'Innovate Corp',
  status: 'Active',
  startDate: '2024-02-01',
  supervisor: 'Jessica Wu',
  cohortId: 'c2',
  email: 'sam.p@example.com'
}, {
  id: 's12',
  name: 'Chris Wright',
  company: 'Design Studio',
  status: 'Completed',
  startDate: '2024-01-15',
  supervisor: 'David Kim',
  cohortId: 'c2',
  email: 'chris.w@example.com'
}, {
  id: 's13',
  name: 'Pat Kelly',
  company: 'Cloud Networks',
  status: 'Active',
  startDate: '2024-01-15',
  supervisor: 'Emily Davis',
  cohortId: 'c2',
  email: 'pat.k@example.com'
}, {
  id: 's14',
  name: 'Alex Johnson',
  company: 'FinTech Solutions',
  status: 'Completed',
  startDate: '2024-01-10',
  supervisor: 'Robert Johnson',
  cohortId: 'c2',
  email: 'alex.j@example.com'
},
// Fall 2023
{
  id: 's15',
  name: 'Jordan Taylor',
  company: 'TechFlow Inc.',
  status: 'Completed',
  startDate: '2023-09-01',
  supervisor: 'Sarah Chen',
  cohortId: 'c3',
  email: 'jordan.t@example.com'
}, {
  id: 's16',
  name: 'Casey Davis',
  company: 'DataSystems',
  status: 'Completed',
  startDate: '2023-09-15',
  supervisor: 'Mike Ross',
  cohortId: 'c3',
  email: 'casey.d@example.com'
}, {
  id: 's17',
  name: 'Morgan Miller',
  company: 'Innovate Corp',
  status: 'Completed',
  startDate: '2023-09-01',
  supervisor: 'Jessica Wu',
  cohortId: 'c3',
  email: 'morgan.m@example.com'
}, {
  id: 's18',
  name: 'Riley Wilson',
  company: 'Design Studio',
  status: 'Completed',
  startDate: '2023-09-10',
  supervisor: 'David Kim',
  cohortId: 'c3',
  email: 'riley.w@example.com'
}, {
  id: 's19',
  name: 'Quinn Moore',
  company: 'Cloud Networks',
  status: 'Completed',
  startDate: '2023-10-01',
  supervisor: 'Emily Davis',
  cohortId: 'c3',
  email: 'quinn.m@example.com'
}];