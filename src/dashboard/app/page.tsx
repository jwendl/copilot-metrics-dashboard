import { getServerSession } from "next-auth/next";
import { redirect } from 'next/navigation';
import Dashboard, { IProps } from "@/features/dashboard/dashboard-page";
import { Suspense } from "react";
import Loading from "./loading";

export const dynamic = "force-dynamic";

export default async function Home(props: IProps) {
  let id = "initial-dashboard";
  const session = await getServerSession();
  const validUserEmails: string[] = ["juswen@microsoft.com", "altsang@microsoft.com", "katerayner@microsoft.com", "philcousins@microsoft.com", "dermille@microsoft.com", "phlucas@microsoft.com", "relghazi@microsoft.com", "macook@microsoft.com", "markmc@microsoft.com"];

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
