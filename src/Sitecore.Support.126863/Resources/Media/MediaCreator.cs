using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Resources.Media
{
  public class MediaCreator : Sitecore.Resources.Media.MediaCreator
  {
    [NotNull]
    protected override Item CreateItem([NotNull] string itemPath, [NotNull] string filePath, [NotNull] MediaCreatorOptions options)
    {
      Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
      Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
      Assert.ArgumentNotNull(options, "options");

      Item item;

      using (new SecurityDisabler())
      {
        //Database database = this.GetDatabase(options);
        Database database = (Database)typeof(Sitecore.Resources.Media.MediaCreator)
            .GetMethod("GetDatabase", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(this, new object[] { options });

        Item oldItem = options.OverwriteExisting ? database.GetItem(itemPath, options.Language) : null;

        //Item folder = this.GetParentFolder(itemPath, options);
        Item folder = (Item)typeof(Sitecore.Resources.Media.MediaCreator)
            .GetMethod("GetParentFolder", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(this, new object[] { itemPath, options });

        //string itemName = this.GetItemName(itemPath);
        string itemName = (string)typeof(Sitecore.Resources.Media.MediaCreator)
            .GetMethod("GetItemName", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(this, new object[] { itemPath });

        var GetItemTemplateMethod = typeof(Sitecore.Resources.Media.MediaCreator)
            .GetMethod("GetItemTemplate", BindingFlags.NonPublic | BindingFlags.Instance);

        if (oldItem != null && !oldItem.HasChildren && oldItem.TemplateID != TemplateIDs.MediaFolder)
        {
          item = oldItem;
          item.Versions.RemoveAll(true);
          item = item.Database.GetItem(item.ID, item.Language, Sitecore.Data.Version.Latest);
          Assert.IsNotNull(item, "item");

          item.Editing.BeginEdit();
          foreach (Field field in item.Fields)
          {
            field.Reset();
          }

          item.Editing.EndEdit();

          item.Editing.BeginEdit();

          if (string.Equals(item.Name, itemName, StringComparison.InvariantCulture))
          {
            using (new SettingsSwitcher("AllowDuplicateItemNamesOnSameLevel", true.ToString()))
            {
              item.Name = itemName;
            }
          }

          else
            item.Name = itemName;

          //item.TemplateID = this.GetItemTemplate(filePath, options).ID;
          item.TemplateID = ((TemplateItem)GetItemTemplateMethod.Invoke(this, new object[] { filePath, options })).ID;
          item.Editing.EndEdit();
        }
        else
        {
          //item = folder.Add(itemName, this.GetItemTemplate(filePath, options));
          item = folder.Add(itemName, (TemplateItem)GetItemTemplateMethod.Invoke(this, new object[] { filePath, options }));
        }

        Assert.IsNotNull(item, typeof(Item), "Could not create media item: '{0}'.", itemPath);

        Language[] languages = options.Versioned ? new[] { item.Language } : item.Database.Languages;

        string extension = FileUtil.GetExtension(filePath);

        foreach (Language language in languages)
        {
          MediaItem version = item.Database.GetItem(item.ID, language, Sitecore.Data.Version.Latest);

          if (version == null)
          {
            continue;
          }

          using (new EditContext(version, SecurityCheck.Disable))
          {
            version.Extension = StringUtil.GetString(version.Extension, extension);
            version.FilePath = this.GetFullFilePath(item.ID, filePath, itemPath, options);
            version.Alt = StringUtil.GetString(version.Alt, options.AlternateText);
            version.InnerItem.Statistics.UpdateRevision();
          }
        }
      }

      item.Reload();

      return item;
    }
  }
}