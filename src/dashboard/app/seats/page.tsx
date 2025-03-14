import { getServerSession } from "next-auth/next";
import { redirect } from 'next/navigation';
import Dashboard, { IProps } from "@/features/seats/seats-page";
import { Suspense } from "react";
import Loading from "./loading";
import { Metadata } from 'next';
 
export const metadata: Metadata = {
  title: "GitHub Copilot Seats Dashboard",
  description: "GitHub Copilot Seats Dashboard",
};
export const dynamic = "force-dynamic";
export default async function Home(props: IProps) {
  let id = "initial-seats-dashboard";
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

  if (props.searchParams.date ) {
    id = `${id}-${props.searchParams.date}`;
  }

  return (
    <Suspense fallback={<Loading />} key={id}>
      <Dashboard {...props} />
    </Suspense>
  );
}
