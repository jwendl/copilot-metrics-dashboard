import { getServerSession } from "next-auth/next";
import { redirect } from 'next/navigation';
import Dashboard, { IProps } from "@/features/dashboard/dashboard-page";
import { Suspense } from "react";
import Loading from "./loading";

export const dynamic = "force-dynamic";

export default async function Home(props: IProps) {
  let id = "initial-dashboard";
  const session = await getServerSession();
  const validUserEmails: string[] = [];
  for (let i = 0; ; i++) {
    const envVariable = process.env[`APPROVED_ACCOUNTS__${i}`];
    if (envVariable === undefined) break;
    validUserEmails.push(envVariable);
  }

  if (!session) {
    redirect('/api/auth/signin');
  }

  if (!validUserEmails.includes(session.user?.email as string)) {
    redirect('/api/auth/signin');
  }
      
  if (props.searchParams.startDate && props.searchParams.endDate) {
    id = `${id}-${props.searchParams.startDate}-${props.searchParams.endDate}`;
  }

  return (
    <Suspense fallback={<Loading />} key={id}>
      <Dashboard {...props} />
    </Suspense>
  )
}
