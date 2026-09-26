import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";

function AdminDashboardPage() {
  return (
    <div className="admin-page">
      <header className="admin-header">
        <div className="admin-header-content">
          <Link to="/admin" aria-label="Knight School admin dashboard">
            <img
              className="admin-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav className="admin-navigation" aria-label="Admin navigation">
            <Link to="/admin" aria-current="page">
              Dashboard
            </Link>
            <Link to="/profile">My profile</Link>
            <Link to="/login">Sign out</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <p className="admin-eyebrow">Administration portal</p>

        <h1 className="admin-title">Admin Dashboard</h1>

        <p className="admin-description">
          Manage participants, instructors, classes, skills, and training
          progress.
        </p>

        <section
          className="admin-summary-grid"
          aria-label="Dashboard summary"
        >
          <article className="admin-summary-card">
            <span className="admin-summary-number">42</span>
            <span className="admin-summary-label">Active participants</span>
          </article>

          <article className="admin-summary-card">
            <span className="admin-summary-number">6</span>
            <span className="admin-summary-label">Instructors</span>
          </article>

          <article className="admin-summary-card">
            <span className="admin-summary-number">8</span>
            <span className="admin-summary-label">Active classes</span>
          </article>

          <article className="admin-summary-card">
            <span className="admin-summary-number">5</span>
            <span className="admin-summary-label">Skills awaiting review</span>
          </article>
        </section>

        <div className="admin-content-grid">
          <section className="admin-panel" aria-labelledby="roster-heading">
            <div className="admin-panel-heading">
              <h2 id="roster-heading">Participant roster</h2>

              <button className="admin-button" type="button">
                Add participant
              </button>
            </div>

            <div className="admin-table-wrapper">
              <table
                className="admin-table"
                aria-labelledby="roster-heading"
              >
                <thead>
                  <tr>
                    <th scope="col">Participant</th>
                    <th scope="col">Program</th>
                    <th scope="col">Progress</th>
                    <th scope="col">Status</th>
                  </tr>
                </thead>

                <tbody>
                  <tr>
                    <td>Alex Morgan</td>
                    <td>Stage Combat Foundations</td>
                    <td>65%</td>
                    <td>
                      <span className="admin-status admin-status-active">
                        Active
                      </span>
                    </td>
                  </tr>

                  <tr>
                    <td>Jordan Lee</td>
                    <td>Historical Fencing</td>
                    <td>82%</td>
                    <td>
                      <span className="admin-status admin-status-active">
                        Active
                      </span>
                    </td>
                  </tr>

                  <tr>
                    <td>Sam Taylor</td>
                    <td>Mounted Combat</td>
                    <td>40%</td>
                    <td>
                      <span className="admin-status admin-status-review">
                        Review
                      </span>
                    </td>
                  </tr>

                  <tr>
                    <td>Casey Williams</td>
                    <td>Fight Choreography</td>
                    <td>73%</td>
                    <td>
                      <span className="admin-status admin-status-active">
                        Active
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <aside className="admin-panel" aria-labelledby="actions-heading">
            <div className="admin-panel-heading">
              <h2 id="actions-heading">Quick actions</h2>
            </div>

            <ul className="admin-actions-list">
              <li>
                <button className="admin-quick-action" type="button">
                  Import participant roster
                </button>
              </li>

              <li>
                <button className="admin-quick-action" type="button">
                  Manage instructors
                </button>
              </li>

              <li>
                <button className="admin-quick-action" type="button">
                  Manage classes
                </button>
              </li>

              <li>
                <button className="admin-quick-action" type="button">
                  Manage skill tree
                </button>
              </li>

              <li>
                <button className="admin-quick-action" type="button">
                  Review media uploads
                </button>
              </li>
            </ul>
          </aside>
        </div>
      </main>
    </div>
  );
}

export default AdminDashboardPage;