﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using eLifeWEB.Utils;
using eLifeWEB.Models;
using DHTMLX.Scheduler;
using DHTMLX.Scheduler.Data;
using DHTMLX.Common;
using DHTMLX.Scheduler.Controls;
using Microsoft.AspNet.Identity;
using System.Text;
using Newtonsoft.Json;

namespace eLifeWEB.Controllers.WEBControllers
{
    public class DoctorInformsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: DoctorInforms
        public ActionResult Index(string searchString, string specializations)
        {
            var doctorInforms = db.DoctorInforms.Include(d => d.Clinic).Where(q =>q.Practiced);
            SelectList specialiation = new SelectList(new Specializations().specializations);
            ViewBag.Specialization = specialiation;
            if (!String.IsNullOrEmpty(searchString))
            {
                doctorInforms = doctorInforms.Where(s => s.ApplicationUsers.FirstOrDefault().Name.ToUpper().Contains(searchString.ToUpper())
                                       || s.Clinic.Name.ToUpper().Contains(searchString.ToUpper()));
            }
            if (!String.IsNullOrEmpty(specializations))
            {
                doctorInforms = doctorInforms.Where(s => s.Specialization == specializations);
            }
            return View(doctorInforms.ToList());
        }

        // GET: DoctorInforms/Details/5
        public ActionResult Details(int? id)
        {
            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DoctorInform doctorInform = db.DoctorInforms.Find(id);
            if (doctorInform == null)
            {
                return HttpNotFound();
            }
            var scheduler = new DHXScheduler(this);
            scheduler.Skin = DHXScheduler.Skins.Material;
            scheduler.LoadData = true;
            scheduler.EnableDataprocessor = true;
            scheduler.Config.first_hour = 6;
            scheduler.Config.last_hour = 20;
            scheduler.Data.Loader.AddParameter("id", id);
            scheduler.Localization.Set(SchedulerLocalization.Localizations.Ukrainian);
            scheduler.Config.drag_lightbox = true;
            scheduler.Lightbox.Clear();
            var select = new LightboxSelect("service", "Послуга");
            var services = new List<object>();
            foreach (TypeOfService type in doctorInform.ApplicationUsers.FirstOrDefault().TypeOfServices)
            {
                services.Add(new { key = type.Id, label = type.Type.Type1 });
            }
            scheduler.Config.icons_select = null;
            scheduler.Config.drag_create = false;
            scheduler.Config.drag_lightbox = false;
            scheduler.Config.drag_resize = false;
            scheduler.Config.drag_move = false;
            var items = services;
            select.AddOptions(items);
            scheduler.Lightbox.Add(select);
            scheduler.Config.buttons_left = new LightboxButtonList
            {
                new EventButton
                {
                    Label = "Записатися на прийом",
                    OnClick = "appointment",
                    Name = "appointment"
                },
                LightboxButtonList.Cancel,
            };
            scheduler.Config.buttons_right = new LightboxButtonList();
            ViewBag.Scheduler = scheduler;
            return View(doctorInform);
        }

        public ContentResult Data(int? id)
        {
            List<object> list = new List<object>();
            ApplicationDbContext db = new ApplicationDbContext();
            var records = new ApplicationDbContext().Records.Where((d => d.TypeOfService.Doctor.DoctorInform.Id == id && d.Patient == null));

            foreach (Record record in records)
            {
                list.Add(new { id = record.Id, text = "Вільне місце", start_date = record.Date, end_date = record.Date.AddHours(2) });

            }
            return new SchedulerAjaxData(list);

        }

        public ContentResult Save(int? id, FormCollection actionValues)
        {
            var action = new DataAction(actionValues);
            try
            {
                return (new AjaxSaveResponse(action));
            }
            finally
            {

                RedirectToAction("Index");
            }
        }

        private ActionResult RedirectAfterSaving()
        {
            return RedirectToAction("Index", new { target = "_blank"});
        }
        

        // GET: DoctorInforms/Create
        public ActionResult Create()
        {
            ViewBag.Id_clinic = new SelectList(db.Clinics, "Id_clinic", "Name");
            return View();
        }

        // POST: DoctorInforms/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Specialization,Category_,Guardian,Id_clinic,Education,Skills,Practiced,Photo")] DoctorInform doctorInform)
        {
            if (ModelState.IsValid)
            {
                db.DoctorInforms.Add(doctorInform);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Id_clinic = new SelectList(db.Clinics, "Id_clinic", "Name", doctorInform.ClinicId);
            return View(doctorInform);
        }

        // GET: DoctorInforms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DoctorInform doctorInform = db.DoctorInforms.Find(id);
            if (doctorInform == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id_clinic = new SelectList(db.Clinics, "Id_clinic", "Name", doctorInform.ClinicId);
            return View(doctorInform);
        }

        // POST: DoctorInforms/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Specialization,Category_,Guardian,Id_clinic,Education,Skills,Practiced,Photo")] DoctorInform doctorInform)
        {
            if (ModelState.IsValid)
            {
                db.Entry(doctorInform).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Id_clinic = new SelectList(db.Clinics, "Id_clinic", "Name", doctorInform.ClinicId);
            return View(doctorInform);
        }

        public ActionResult AcceptAppointment(int? id, int? serviceId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //ViewBag.Service = serviceId;
            //Record record = db.Records.Find(id);
            //return View(record);
            Record record = db.Records.Find(id);
            ApplicationUser Patient = db.Users.Find(User.Identity.GetUserId());
            TypeOfService typeOfService = db.TypeOfServices.Where(u => u.Id == serviceId && u.DoctorId == record.DoctorId).FirstOrDefault();
            return View("Payment", LiqPayHelper.GetLiqPayModel(record, typeOfService, Patient/*, card_cvv, card_exp_month, card_exp_year*/));

        }

        [HttpPost]
        public ActionResult AppointentResult()
        {
            var request_dictionary = Request.Form.AllKeys.ToDictionary(key => key, key => Request.Form[key]);

            // --- Розшифровую параметр data відповіді LiqPay та перетворюю в Dictionary<string, string> для зручності:
            byte[] request_data = Convert.FromBase64String(request_dictionary["data"]);
            string decodedString = Encoding.UTF8.GetString(request_data);
            var request_data_dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedString);

            // --- Отримую сигнатуру для перевірки
            var mySignature = LiqPayHelper.GetLiqPaySignature(request_dictionary["data"]);

            // --- Якщо сигнатура серевера не співпадає з сигнатурою відповіді LiqPay - щось пішло не так
            if (mySignature != request_dictionary["signature"])
                return View("~/Views/Shared/_Error.cshtml");

            // --- Якщо статус відповіді "Тест" або "Успіх" - все добре
            if (request_data_dictionary["status"] == "sandbox" || request_data_dictionary["status"] == "success")
            {
                // Тут можна оновити статус замовлення та зробити всі необхідні речі. Id замовлення можна взяти тут: request_data_dictionary[order_id]
                // ...

                return View();
            }
            return View("~/Views/Shared/_Error.cshtml");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
