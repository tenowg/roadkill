﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Web.Security;
using System.IO;

namespace Roadkill.Core.Controllers
{
	public class PageController : ControllerBase
    {
		public ActionResult Index(string id)
		{	
			PageSummary summary = null;

			if (!string.IsNullOrWhiteSpace(id))
			{
				Guid guid = Guid.Empty;
				PageManager manager = new PageManager();
				if (Guid.TryParse(id, out guid))
				{
					summary = manager.GetPage(guid);
				}
				else
				{
					string title = id;
					title = title.Replace("-", " ");
					summary = manager.GetPage(title);
				}
			}

			if (summary == null)
				return new HttpNotFoundResult(string.Format("The page with title or id '{0}' could not be found",id));

			SetPageTitle(summary.Title);

			return View(summary);
		}

		public ActionResult History(Guid id)
		{
			SetPageTitle("Version history");
			HistoryManager manager = new HistoryManager();
			return View(manager.GetHistory(id).ToList());
		}

		public ActionResult Version(Guid id)
		{
			HistoryManager manager = new HistoryManager();
			IList<PageSummary> list = manager.CompareVersions(id).ToList();

			SetPageTitle("Version number" + list[0].VersionNumber);
			return View(list);
		}

		public ActionResult Revert(Guid versionId,Guid pageId)
		{
			HistoryManager manager = new HistoryManager();
			manager.RevertTo(versionId);

			return RedirectToAction("History", new { id = pageId });
		}

		public ActionResult AllPages()
		{
			SetPageTitle("All pages");

			PageManager manager = new PageManager();
			return View(manager.AllPages());
		}

		public ActionResult ByUser(string id)
		{
			SetPageTitle("Pages created by "+id);
			ViewData["Username"] = id;

			PageManager manager = new PageManager();
			return View(manager.AllPagesCreatedBy(id));
		}

		public ActionResult AllTags()
		{
			SetPageTitle("All tags");

			PageManager manager = new PageManager();
			return View(manager.AllTags());
		}

		public ActionResult Tag(string id)
		{
			SetPageTitle("All [" +id+ "] pages");

			PageManager manager = new PageManager();
			return View(manager.FindByTag(id));
		}

		[Authorize]
		public ActionResult New()
		{
			SetPageTitle("New page");
			return View("Edit", new PageSummary());
		}

		[Authorize]
		[HttpPost]
		[ValidateInput(false)]
		public ActionResult New(PageSummary summary)
		{
			if (!ModelState.IsValid)
				return View("Edit", summary);

			PageManager manager = new PageManager();
			summary = manager.AddPage(summary);

			return RedirectToAction("Index", new { id = summary.Id });
		}

		[Authorize]
		public ActionResult Edit(Guid id)
		{
			PageManager manager = new PageManager();
			PageSummary summary = manager.GetPage(id);

			if (summary != null)
			{
				SetPageTitle("Editing '" + summary.Title + "'");
				return View("Edit", summary);
			}
			else
			{
				return RedirectToAction("New");
			}
		}

		[Authorize]
		[HttpPost]
		[ValidateInput(false)]
		public ActionResult Edit(PageSummary summary)
		{
			if (!ModelState.IsValid)
				return View("Edit", summary);

			PageManager manager = new PageManager();
			manager.UpdatePage(summary);

			return RedirectToAction("Index", new { id = summary.Id });
		}

		[Authorize]
		public ActionResult Delete(Guid id)
		{
			PageManager manager = new PageManager();
			manager.DeletePage(id);

			return RedirectToAction("AllPages");
		}

		[Authorize]
		public ActionResult AllFiles(Guid id)
		{
			SetPageTitle("File explorer");
			ViewData["id"] = id;
			ViewData["AttachmentPath"] = VirtualPathUtility.ToAbsolute("~/" + RoadkillSettings.AttachmentsFolder + "/" + id);

			List<string> files = new List<string>();
			string folder = string.Format(@"{0}Attachments\{1}", AppDomain.CurrentDomain.BaseDirectory, id);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			else
			{
				foreach (string file in Directory.GetFiles(folder))
				{
					files.Add(Path.GetFileName(file));
				}
			}

			return View(files);
		}


		[Authorize]
		[HttpPost]
		public ActionResult UploadFile(Guid id)
		{
			string folder = string.Format(@"{0}{1}\{2}", AppDomain.CurrentDomain.BaseDirectory, RoadkillSettings.AttachmentsFolder, id);
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			string filePath = string.Format(@"{0}\{1}", folder, Request.Files["uploadFile"].FileName);
			HttpPostedFileBase postedFile = Request.Files["uploadFile"] as HttpPostedFileBase;
			postedFile.SaveAs(filePath);

			return RedirectToAction("AllFiles", new { id = id });
		}

		[Authorize]
		public ActionResult DeleteFile(Guid pageId,string filename)
		{
			string path = string.Format(@"{0}{1}\{2}\{3}", AppDomain.CurrentDomain.BaseDirectory, RoadkillSettings.AttachmentsFolder,pageId, filename);
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);

			return RedirectToAction("AllFiles", new { id = pageId });
		}
    }
}