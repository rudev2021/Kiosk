// RU - Kiosks
// RU - ru@college.ucla.edu - Jun 2019   (ru: #53047)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using M5.Areas.Workgroup.Models;


namespace M5.Areas.Workgroup.Controllers
{

    public class KioskController : Controller
    {
        private DataClassesDataContext db = DBUtil.getStudentsDataContext();

        Shared shared = new Shared();

        // GET: Workgroup/Kiosk
        public ActionResult Kiosk(int? id)
        {
            // This line sets up the data in the viewbag for the user's name, menu, home link, etc...

            CIS.MyUCLA.Utilities.PageSetup(ViewBag, Session, SharedController.GROUP_MANAGER_FEATURE_ID, rightPanel: true, leftPanel: true);
            int? calendar_id = null;

            if (!shared.IsUnitAdmin(shared.UnitIdCurrent))
                return new HttpUnauthorizedResult();

            if (id == null)
            {
                return RedirectToAction("Events", "Events");
            }
            else
            {
                calendar_id = id;
            }

            KioskAPI api = new KioskAPI();
            bool isKioskMgr = api.isKioskManager(shared.UclaLogon, shared.DeptCodeCurrent);
            ViewBag.isKioskMgr = isKioskMgr;
            KioskView m = getViewModel(calendar_id);

            if (m == null)
            {
                return RedirectToAction("Events", "Events");
            }

            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Kiosk(KioskView model)
        {
            // This line sets up the data in the viewbag for the user's name, menu, home link, etc...
            CIS.MyUCLA.Utilities.PageSetup(ViewBag, Session, SharedController.GROUP_MANAGER_FEATURE_ID, rightPanel: true, leftPanel: true);

            if (Request.Form["Enable"] != null)
            {
                return EnableKiosk(model.calendarID);
            }
            return Kiosk(model.calendarID);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EnableKiosk(int? calendarID)
        {
            string message = null;

            // This line sets up the data in the viewbag for the user's name, menu, home link, etc...

            CIS.MyUCLA.Utilities.PageSetup(ViewBag, Session, SharedController.GROUP_MANAGER_FEATURE_ID, rightPanel: true, leftPanel: true);

            if (!shared.IsUnitAdmin(shared.UnitIdCurrent))
            {
                return new HttpUnauthorizedResult();
            }

            KioskAPI api = new KioskAPI();

            try
            {
                if (calendarID == null)
                {
                    return RedirectToAction("Events", "Events");
                }

                EventsViewItem o = shared.getEventsViewItem(calendarID);

                if (o == null || !o.hasEvents)
                {
                    return RedirectToAction("Events", "Events");
                }

                if (string.IsNullOrEmpty(shared.DeptCodeCurrent))
                {
                    message = "deptCode is empty";
                }
                else
                {
                    bool isKioskMgr = api.isKioskManager(shared.UclaLogon, shared.DeptCodeCurrent);

                    if (isKioskMgr)
                    {
                        var kstat = api.enableKiosk(calendarID);

                        if (kstat == null || kstat.eventKioskID == null)
                            message = "failed to enable kiosk";

                        else
                            message = "successfully enabled kiosk";
                    }
                    else
                        message = "unable to enable kiosk.";
                }
            }
            catch (Exception ex)
            {
                message = "failed to enable kiosk.  ";  // ru: #53047
                string mname = System.Environment.MachineName.ToLower();
                if (mname.IndexOf("webmyucla70") >= 0)
                    message += ex.Message;
            }
            finally
            {
                ViewBag.alertMsg = message;
            }

            return Kiosk(calendarID);
        }


        private KioskView getViewModel(int? calendarID)
        {
            return getViewModel(new KioskView { calendarID = calendarID });
        }


        private KioskView getViewModel(KioskView model)
        {
            if (model == null)
            {
                return null;
            }

            if (model.calendarID == null)
            {
                return null;
            }

            EventsViewItem o = shared.getEventsViewItem(model.calendarID);

            if (o == null)
            {
                return null;
            }

            ViewBag.featureEnabled = (o.hasEvents);

            if (ModelState.IsValid)
            {
                KioskView m = new KioskView { calendarID = model.calendarID, unitID = shared.UnitIdCurrent };
                ViewBag.backLink = Url.Action("Events", "Events", new { id = model.unitID });
                bool hasDeptCode = !string.IsNullOrEmpty(shared.DeptCodeCurrent);
                var e = shared.resEventGet(model.calendarID);

                if (e == null)
                    return Redirect(ViewBag.backLink);

                ViewBag.Title = ViewBag.pageName = "Event Kiosk";

                model.eventName = e.itemTitle;
                model.unitID = shared.UnitIdCurrent;
                model.term = e.term;
                model.key = e.eventKioskAccessKey;
                model.kioskID = e.eventKioskEventID;
                model.enabled = e.eventKioskEventID != null && hasDeptCode;

                ViewBag.hasDeptCode = hasDeptCode;

                List<EventMember> list = AttendView.getMembers(shared.CisidUser.ToString(), model.calendarID);
                int eCount = (from oe in list where oe.enrollStatus == "E" select oe).Count();
                int wCount = (from ow in list where ow.enrollStatus == "W" select ow).Count();
                List<ReserveType> rtypes = new List<ReserveType>();
                rtypes.Add(new ReserveType { type = "E", typeName = "Enrolled", enabled = true, count = eCount });
                rtypes.Add(new ReserveType { type = "W", typeName = "Waitlisted", enabled = false, count = wCount });
                model.reserveTypes = rtypes;
                ViewBag.title = model.eventName;

                return model;
            }
            else
                return null;
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SendList(int? calendarID, bool? enrolled, bool? waitlisted)
        {
            bool status = false;
            string message = "";

            if (!shared.IsUnitAdmin(shared.UnitIdCurrent))
                return new HttpUnauthorizedResult();

            if (enrolled == null)
                enrolled = false;


            if (waitlisted == null)
                waitlisted = false;

            if (enrolled == false && waitlisted == false)
            {
                message = "must select enrolled and/or wailisted";
            }
            else
            {
                try
                {
                    EventsViewItem o = shared.getEventsViewItem(calendarID);

                    if (o == null)
                    {
                        message = "no matching event";
                    }
                    else if (o.hasEvents && !string.IsNullOrEmpty(shared.DeptCodeCurrent))
                    {

                        var e = shared.resEventGet(calendarID);   // get event specific properties

                        if (e.eventKioskEventID == null)
                        {
                            message = "no kisok info found";
                        }
                        else
                        {
                            KioskAPI api = new KioskAPI();

                            bool isKioskMgr = api.isKioskManager(shared.UclaLogon, shared.DeptCodeCurrent);

                            if (isKioskMgr)  /* not requiring kiosk manager role */
                            {
                                api.addRsvpUIDs(calendarID, enrolled, waitlisted);
                                status = true;
                                message = "sent list to kiosk successfully";
                            }
                            else
                            {
                                status = false;
                                message = "unable to sent list to kiosk";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = false;
                    message = "unable to send list to kiosk.  ";   // ru: #53047
                    string mname = System.Environment.MachineName.ToLower();

                    if (mname.IndexOf("webmyucla70") >= 0)
                        message += ex.Message;
                }
            }

            // Return JSON Data
            return Json(new
            {
                calendarID = calendarID,
                status = status,
                message = message
            });

        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateKiosk(int? calendarID)
        {
            bool status = false;
            string message = "";

            if (!shared.IsUnitAdmin(shared.UnitIdCurrent))
                return new HttpUnauthorizedResult();

            try
            {
                EventsViewItem o = shared.getEventsViewItem(calendarID);
                if (o == null)
                {
                    message = "no matching event";
                }
                else if (o.hasEvents && !string.IsNullOrEmpty(shared.DeptCodeCurrent))
                {
                    var e = shared.resEventGet(calendarID);   // get event specific properties
                    if (e.eventKioskEventID == null)
                    {
                        message = "no kisok info found";
                    }
                    else
                    {
                        KioskAPI api = new KioskAPI();
                        bool isKioskMgr = api.isKioskManager(shared.UclaLogon, shared.DeptCodeCurrent);

                        if (isKioskMgr)
                        {
                            api.updateEvent(calendarID);
                            status = true;
                            message = "updated kiosk successfully";
                        }
                        else
                        {
                            status = false;
                            message = "unable to update kiosk";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "unable to update kiosk.  ";  // ru: #53047
                string mname = System.Environment.MachineName.ToLower();

                if (mname.IndexOf("webmyucla70") >= 0)
                    message += ex.Message;
            }

            // Return JSON Data
            return Json(new
            {
                calendarID = calendarID,
                status = status,
                message = message
            });
        }

    }

}