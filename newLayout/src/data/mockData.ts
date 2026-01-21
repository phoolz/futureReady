export type Status = 'Active' | 'Pending' | 'Completed';
export interface Student {
  id: string;
  name: string;
  company: string;
  status: Status;
  startDate: string;
  supervisor: string;
  email: string;
}
export const students: Student[] = [
{
  id: 's1',
  name: 'Alex Thompson',
  company: 'TechFlow Inc.',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Sarah Chen',
  email: 'alex.t@example.com'
}, {
  id: 's2',
  name: 'Jordan Lee',
  company: 'DataSystems',
  status: 'Pending',
  startDate: '2024-09-15',
  supervisor: 'Mike Ross',
  email: 'jordan.l@example.com'
}, {
  id: 's3',
  name: 'Casey Smith',
  company: 'Innovate Corp',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Jessica Wu',
  email: 'casey.s@example.com'
}, {
  id: 's4',
  name: 'Taylor Brown',
  company: 'Design Studio',
  status: 'Active',
  startDate: '2024-09-10',
  supervisor: 'David Kim',
  email: 'taylor.b@example.com'
}, {
  id: 's5',
  name: 'Morgan Wilson',
  company: 'Cloud Networks',
  status: 'Pending',
  startDate: '2024-10-01',
  supervisor: 'Emily Davis',
  email: 'morgan.w@example.com'
}, {
  id: 's6',
  name: 'Riley Garcia',
  company: 'FinTech Solutions',
  status: 'Active',
  startDate: '2024-09-05',
  supervisor: 'Robert Johnson',
  email: 'riley.g@example.com'
}, {
  id: 's7',
  name: 'Quinn Martinez',
  company: 'HealthPlus',
  status: 'Active',
  startDate: '2024-09-01',
  supervisor: 'Lisa Anderson',
  email: 'quinn.m@example.com'
}, {
  id: 's8',
  name: 'Avery Robinson',
  company: 'EcoEnergy',
  status: 'Pending',
  startDate: '2024-09-20',
  supervisor: 'Tom White',
  email: 'avery.r@example.com'
}, {
  id: 's9',
  name: 'Jamie Clark',
  company: 'TechFlow Inc.',
  status: 'Completed',
  startDate: '2024-01-15',
  supervisor: 'Sarah Chen',
  email: 'jamie.c@example.com'
}, {
  id: 's10',
  name: 'Drew Rodriguez',
  company: 'DataSystems',
  status: 'Completed',
  startDate: '2024-01-20',
  supervisor: 'Mike Ross',
  email: 'drew.r@example.com'
}, {
  id: 's11',
  name: 'Sam Patels',
  company: 'Innovate Corp',
  status: 'Active',
  startDate: '2024-02-01',
  supervisor: 'Jessica Wu',
  email: 'sam.p@example.com'
}, {
  id: 's12',
  name: 'Chris Wright',
  company: 'Design Studio',
  status: 'Completed',
  startDate: '2024-01-15',
  supervisor: 'David Kim',
  email: 'chris.w@example.com'
}, {
  id: 's13',
  name: 'Pat Kelly',
  company: 'Cloud Networks',
  status: 'Active',
  startDate: '2024-01-15',
  supervisor: 'Emily Davis',
  email: 'pat.k@example.com'
}, {
  id: 's14',
  name: 'Alex Johnson',
  company: 'FinTech Solutions',
  status: 'Completed',
  startDate: '2024-01-10',
  supervisor: 'Robert Johnson',
  email: 'alex.j@example.com'
}, {
  id: 's15',
  name: 'Jordan Taylor',
  company: 'TechFlow Inc.',
  status: 'Completed',
  startDate: '2023-09-01',
  supervisor: 'Sarah Chen',
  email: 'jordan.t@example.com'
}, {
  id: 's16',
  name: 'Casey Davis',
  company: 'DataSystems',
  status: 'Completed',
  startDate: '2023-09-15',
  supervisor: 'Mike Ross',
  email: 'casey.d@example.com'
}, {
  id: 's17',
  name: 'Morgan Miller',
  company: 'Innovate Corp',
  status: 'Completed',
  startDate: '2023-09-01',
  supervisor: 'Jessica Wu',
  email: 'morgan.m@example.com'
}, {
  id: 's18',
  name: 'Riley Wilson',
  company: 'Design Studio',
  status: 'Completed',
  startDate: '2023-09-10',
  supervisor: 'David Kim',
  email: 'riley.w@example.com'
}, {
  id: 's19',
  name: 'Quinn Moore',
  company: 'Cloud Networks',
  status: 'Completed',
  startDate: '2023-10-01',
  supervisor: 'Emily Davis',
  email: 'quinn.m@example.com'
}];
