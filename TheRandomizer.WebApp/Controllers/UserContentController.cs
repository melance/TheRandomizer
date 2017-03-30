﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TheRandomizer.Generators.Assignment;
using TheRandomizer.Generators.Dice;
using TheRandomizer.Generators.List;
using TheRandomizer.Generators.LUA;
using TheRandomizer.Generators.Phonotactics;
using TheRandomizer.Generators.Parameter;
using TheRandomizer.Generators.Table;
using TheRandomizer.WebApp.Models;
using TheRandomizer.Generators;
using System.Web.Script.Serialization;
using System.Text;

namespace TheRandomizer.WebApp.Controllers
{
    [RequireHttps]
    [Authorize]
    public class UserContentController : Controller
    {
        public static List<GeneratorTypeModel> GeneratorTypes
        {
            get
            {
                List<GeneratorTypeModel> value = new List<GeneratorTypeModel>()
                    {
                        new GeneratorTypeModel(typeof(AssignmentGenerator), "AssignmentEditor"),
                        new GeneratorTypeModel(typeof(DiceGenerator), "DiceEditor"),
                        new GeneratorTypeModel(typeof(ListGenerator), "ListEditor"),
                        new GeneratorTypeModel(typeof(LUAGenerator), "LuaEditor"),
                        new GeneratorTypeModel(typeof(PhonotacticsGenerator), "PhonotacticsEditor"),
                        new GeneratorTypeModel(typeof(TableGenerator), "TableEditor")
                    };
                return value.OrderBy(gtm => gtm.Name).ToList();
            }
        }
        

        #region Base Generator
        [HttpPost]
        public string GetGeneratorTypes()
        {
            var result = new Dictionary<string, string>();
            foreach (var type in GeneratorTypes)
            {
                result.Add(type.Name, type.Action);
            }
            return new JavaScriptSerializer().Serialize(result);
        }

        public ActionResult CreateParameter(Int32 index)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"Parameters[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/Configuration.cshtml");
        }

        public ActionResult EditGenerator(Int32 Id)
        {
            var model = DataAccess.DataContext.GetGenerator(Id);
            var modelType = GeneratorTypes.Find(gtm => gtm.Type == model.GetType());
            
            return View(modelType.Action, model);
        }

        public ActionResult SelectGeneratorType()
        {                      
            return View(GeneratorTypes);            
        }

        [HttpPost]
        public ActionResult SelectGeneratorType(HttpPostedFileBase upload)
        {
            if (upload == null || upload.ContentLength == 0)
            {
                // No or Empty File Error
                ViewBag.UploadError = "There was an error retrieving the file, either no file was found or the file was zero length.";
            }
            else if (upload.ContentType != "text/xml")
            {
                // Wrong content type error
                ViewBag.UploadError = $"The file provided was of the wrong content type, expcted \"text/xml\" but recieved \"{upload.ContentType}\" content is accepted";
            }
            else
            {
                try
                {
                    // Attempt to deserialize the generator
                    var length = (Int32)upload.InputStream.Length;
                    byte[] bytes = new byte[length];
                    string xml;
                    object model = null;
                    GeneratorTypeModel modelType;

                    upload.InputStream.Read(bytes, 0, length);

                    xml = System.Text.Encoding.UTF8.GetString(bytes);

                    // Check if this is a library file
                    if (upload.FileName.EndsWith(".lib.xml"))
                    {
                        //Todo: deserialize Libraries
                    }
                    else
                    {
                        model = BaseGenerator.Deserialize(xml);
                    }

                    // Open appropriate editor with these contents
                    modelType = GeneratorTypes.Find(gtm => gtm.Type == model.GetType());

                    if (modelType == null)
                    {
                        ViewBag.UploadError = $"Unrecognized generator type: {model.GetType().Name}";
                    }
                    else
                    {
                        return View(modelType.Action, model);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.UploadError = $"There was an error trying to read the contents of the file. {ex.Message}";
                }
            }
            return View(GeneratorTypes);
        }

        public ActionResult Export(Int32 id)
        {
            var generator = DataAccess.DataContext.GetGenerator(id);
            var model = generator.Serialize();
            var fileName = generator.Name;
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName.Replace(c, '_');
            }
            fileName += ".rnd.xml";
            return this.File(System.Text.Encoding.UTF8.GetBytes(model), "text/xml", fileName);
        }

        public ActionResult DeleteGenerator(Int32 id)
        {
            try
            {
                if (DataAccess.DataContext.GetGenerator(id) == null) throw new KeyNotFoundException("Unabled to locate the generator.");
                DataAccess.DataContext.DeleteGenerator(id);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unable to delete the generator: {ex.Message}");
                return RedirectToAction("Generate", "Home", new { id = id });
            }
        }
        #endregion

        #region Assignment Generators
        public ActionResult AssignmentEditor()
        {
            var model = new AssignmentGenerator();
            model.LineItems.Add(new LineItem() { Name = "Start" });
            return View(model);
        }

        [HttpPost]
        public ActionResult AssignmentEditor(AssignmentGenerator generator)
        {
            return SaveGenerator(generator);
        }

        public ActionResult CreateLineItem(Int32 index)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"LineItems[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/LineItem.cshtml");
        }
        #endregion

        #region List Generator
        public ActionResult ListEditor()
        {
            return View(new ListGenerator());
        }

        [HttpPost]
        public ActionResult ListEditor(ListGenerator generator)
        {
            return SaveGenerator(generator);
        }
        #endregion

        #region Dice Generator
        public ActionResult DiceEditor()
        {
            var model = new DiceGenerator();
            model.Functions.Add(new RollFunction());
            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DiceEditor(DiceGenerator generator)
        {
            return SaveGenerator(generator);
        }

        public ActionResult CreateRollFunction(Int32 index)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"Functions[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/RollFunction.cshtml");
        }
        #endregion

        #region Lua Generator
        public ActionResult LuaEditor()
        {
            return View(new LUAGenerator());
        }

        [HttpPost]
        public ActionResult LuaEditor(LUAGenerator generator)
        {
            return SaveGenerator(generator);
        }
        #endregion

        #region Phonotactics Generator
        public ActionResult PhonotacticsEditor()
        {
            var model = new PhonotacticsGenerator();
            model.Definitions.Add(new Definition());
            model.Patterns.Add(new Pattern());
            return View(model);
        }

        [HttpPost]
        public ActionResult PhonotacticsEditor(PhonotacticsGenerator generator)
        {
            return SaveGenerator(generator);
        }

        public ActionResult CreateDefinition(Int32 index)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"Definitions[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/Definition.cshtml", new Definition());
        }
        
        public ActionResult CreatePattern(Int32 index)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"Patterns[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/Pattern.cshtml", new Pattern());
        }
        #endregion

        #region Table Generator
        public ActionResult TableEditor()
        {
            var model = new TableGenerator();
            return View(model);
        }

        [HttpPost]
        public ActionResult TableEditor(TableGenerator generator)
        {
            return SaveGenerator(generator);
        }

        public ActionResult CreateRandomTable(Int32 index)
        {
            return CreateTable(index, new RandomTable());
        }

        public ActionResult CreateLoopTable(Int32 index)
        {
            return CreateTable(index, new LoopTable());
        }

        public ActionResult CreateSelectTable(Int32 index)
        {
            return CreateTable(index, new SelectTable());
        }

        private ActionResult CreateTable(Int32 index, BaseTable model)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"Tables[{index}]";
            return PartialView("~/Views/Shared/EditorTemplates/BaseTable.cshtml", model);
        }
        #endregion

        #region Private Methods
        private ActionResult SaveGenerator(BaseGenerator generator)
        {
            var model = generator;
            if (ModelState.IsValid)
            {
                model = DataAccess.DataContext.UpsertGenerator(generator);
            }
            else
            {
                return View(model);
            }

            if (Request.Form["Submit"].Equals("save", StringComparison.CurrentCultureIgnoreCase))
            {
                return View(model);
            }
            else
            {
                return Redirect($"/Home/Generate/{model.Id}");
            }
        }
        #endregion
    }
}
