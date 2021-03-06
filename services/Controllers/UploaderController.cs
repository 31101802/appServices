﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using quierobesarte.Common;
using quierobesarte.Models;

namespace quierobesarte.Controllers
{
    public class UploaderController : Controller
    {

        public ActionResult Index()
        {
            var model = new UploadImagesDto();
            var weddingId = Request["guid"];


            return Security.ActionResult(weddingId, () =>
                {
                    using (var db = new db498802376Entities())
                    {
                        var singleOrDefault = db.weddings.SingleOrDefault(w => w.id == weddingId);
                        if (singleOrDefault != null)
                        {
                            model.WeddingName = singleOrDefault.name;
                            if (singleOrDefault.active != null)
                            {
                                model.IsWeddingActive = singleOrDefault.active.Value;
                            }
                        }
                    }
                    model.IsAdmin = Session["IsValidPassword"] != null ? (bool) Session["IsValidPassword"] : false;
                }, View(model));
        }


  

        private static AdminDto GetWeddings(AdminDto model)
        {
            model.Weddings = new List<WeddingDto>();
            using (var db = new db498802376Entities())
            {
                foreach (var wedding in db.weddings.OrderByDescending(w => w.date))
                {
                    model.Weddings.Add(new WeddingDto
                        {
                            Name = wedding.name,
                            Date = wedding.date.ToShortDateString(),
                            QrCodeImageName = "~/qrcodes/" + wedding.path,
                            Id = wedding.id
                        });
                }
            }

            return model;
        }

 
        public ActionResult Admin(AdminDto model)
        {

            if (Session["IsValidPassword"] != null || Request["Password"]=="Av@nade123")
            {
                Session["IsValidPassword"] = true;
                model.IsAdmin = true;
                if (!string.IsNullOrWhiteSpace(model.Name) && !string.IsNullOrWhiteSpace(model.Date))
                {

                    var weddingDate = DateTime.MinValue;
                    DateTime.TryParse(model.Date, out weddingDate);

                    model.CurrentQrCodeImageName = QrCodeEngine.Generate(Server.MapPath("~/qrcodes/"), model.Id);

                    using (var db = new db498802376Entities())
                    {


                        if (weddingDate != DateTime.MinValue && model.Name != string.Empty)
                        {


                            db.weddings.Add(new wedding()
                                {
                                    id = model.Id,
                                    date = weddingDate,
                                    name = model.Name,
                                    active = true,
                                    path = model.CurrentQrCodeImageName,
                                    description = string.Empty


                                });
                            db.SaveChanges();

                            model.WeddingCreated = true;
                        }


                    }

                   


                }
                model = GetWeddings(model);
            }
            return View(model);
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult Upload()
        {
            try
            {
                Utils.LogRequestHeaders("UploaderController-Upload");
            }
            catch (Exception)
            {

                return new HttpStatusCodeResult(HttpStatusCode.UpgradeRequired);
            }


            var filesUploaded = new List<object>();
            string weddingId = Request["guid"];

            using (var db = new db498802376Entities())
            {
                var result = db.weddings.SingleOrDefault(w => w.id == weddingId && w.active.Value);
                if (result == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                }
                for (var i = 0; i < this.Request.Files.Count; i++)
                {
                    var file = this.Request.Files[i];
                    var fileName = Path.GetFileName(file.FileName);
                    var photo = WebImage.GetImageFromRequest();

                    if (photo != null)
                    {
                        var userThumbnail = photo.Clone().Resize(width: 380, height: 380, preserveAspectRatio: true,
                                                                 preventEnlarge: true);


                        if (System.IO.File.Exists(Server.MapPath("~/uploads/" + fileName)))
                        {
                            fileName = FileHelper.GetUniuqueFileName(Server.MapPath("~/uploads/"), fileName);
                        }
                        Directory.CreateDirectory(Server.MapPath("~/uploads/Thumbnail/"));
                        userThumbnail.Save(Server.MapPath("~/uploads/Thumbnail/" + fileName));
                        Directory.CreateDirectory(Server.MapPath("~/uploads"));

                        photo.Save(Server.MapPath("~/uploads/" + fileName));

                        db.wedding_image.Add(new wedding_image()
                                                 {
                                                     wedding_id = weddingId,
                                                     id = Utils.GenerateUnique(),
                                                     name = fileName,
                                                     user_created = Utils.GetUser(),
                                                     created = DateTime.Now
                                                 });
                        db.SaveChanges();
                    }

                    filesUploaded.Add(new { name = fileName });
                }
                var files = new { files = filesUploaded };
                return new JsonResult { Data = files };
            }
        }


        public ActionResult Viewer()
        {
            var model = new WeddingImageViewerDto();
            var weddingId = Request["guid"];
            return Security.ActionResult(weddingId, () =>
            {

                model.Images = new List<WeddingImageDto>();
                using (var db = new db498802376Entities())
                {
                    var singleOrDefault = db.weddings.SingleOrDefault(w => w.id == weddingId);

                    if (singleOrDefault != null)
                    {
                        model.WeddingName = singleOrDefault.name;
                        model.WeddingId = singleOrDefault.id;
                        foreach (var image in db.wedding_image.Where(w => w.wedding_id == weddingId).OrderByDescending(w => w.created))
                        {
                            model.Images.Add(new WeddingImageDto
                            {
                                Id = image.id.ToString(),
                                OriginalPath = "~/uploads/" + image.name,
                                ThumbnailPath = "~/uploads/Thumbnail/" + image.name,
                                Comment = image.comment,

                            });
                        }
                    }

                }
            }, View(model));


        }

        public ActionResult AdminViewer()
        {

            var model = new WeddingImageViewerDto();
            var weddingId = Request["guid"];
            return Security.ActionResult(weddingId, () =>
            {
                decimal  imageToDelete;
                decimal.TryParse(Request["imageToDelete"], out imageToDelete);
                if (imageToDelete>0)
                {
                    using (var db = new db498802376Entities())
                    {

                        db.wedding_image.Remove(db.wedding_image.SingleOrDefault(wi => wi.id == imageToDelete));
                        db.SaveChanges();

                    }
                }

                model.Images = new List<WeddingImageDto>();
                using (var db = new db498802376Entities())
                {
                    var singleOrDefault = db.weddings.SingleOrDefault(w => w.id == weddingId);

                    if (singleOrDefault != null)
                    {
                        model.WeddingName = singleOrDefault.name;
                        model.WeddingId = singleOrDefault.id;
                        foreach (var image in db.wedding_image.Where(w => w.wedding_id == weddingId).OrderByDescending(w => w.created))
                        {
                            model.Images.Add(new WeddingImageDto
                            {
                                Id = image.id.ToString(),
                                OriginalPath = "~/uploads/" + image.name,
                                ThumbnailPath = "~/uploads/Thumbnail/" + image.name,
                                Comment = image.comment,

                            });
                        }
                    }

                }
            }, View(model));


        }





    }
}
