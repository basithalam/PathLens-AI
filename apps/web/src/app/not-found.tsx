import Link from "next/link";

export default function NotFound() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Not in the dataset</div>
        <h1>This page does not exist — yet</h1>
        <p>
          Either the address is wrong or the report was deleted at its owner&rsquo;s request. PathLens does
          not guess at what might have been here, and it does not fill the gap with a similar-looking record.
        </p>
      </div>

      <dl className="meta-grid">
        <div>
          <dt>Browse what is known</dt>
          <dd>
            <Link href="/reports">All reports</Link>
            <small>Community claims with their verification status.</small>
          </dd>
        </div>
        <div>
          <dt>Look at a place</dt>
          <dd>
            <Link href="/map">Accessibility map</Link>
            <small>Pan to an area and its reports reload for that viewport.</small>
          </dd>
        </div>
        <div>
          <dt>Plan around it</dt>
          <dd>
            <Link href="/routes">Route planner</Link>
            <small>Ranked by evidence, with the unknown share stated.</small>
          </dd>
        </div>
        <div>
          <dt>Add the missing evidence</dt>
          <dd>
            <Link href="/report/new">Report an obstacle</Link>
            <small>Starts unverified, as it should.</small>
          </dd>
        </div>
      </dl>

      <p className="callout callout--unknown">
        <strong>Unknown is a real answer.</strong> An empty result here means no one has reported this place
        — it never means the path was checked and found clear.
      </p>
    </>
  );
}
