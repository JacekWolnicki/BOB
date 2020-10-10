﻿using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Static class to manage all-building prop and tree replacements.
	/// </summary>
	internal static class AllBuildingReplacement
	{
		// Master dictionaries of currently active all-building replacements.
		internal static Dictionary<PrefabInfo, PrefabInfo> propReplacements;
		internal static Dictionary<PrefabInfo, PrefabInfo> treeReplacements;

		// Master dictionary of active all-building replacements currently applied to building prefabs.
		internal static Dictionary<BuildingInfo, SortedList<int, Replacement>> allBuildingDict;



		/// <summary>
		/// Performs setup and initialises the master dictionaries.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			allBuildingDict = new Dictionary<BuildingInfo, SortedList<int, Replacement>>();
			propReplacements = new Dictionary<PrefabInfo, PrefabInfo>();
			treeReplacements = new Dictionary<PrefabInfo, PrefabInfo>();
		}


		/// <summary>
		/// Reverts all active all-building replacements and re-initialises the master dictionaries.
		/// </summary>
		internal static void RevertAll()
		{
			// Iterate through each entry in the master tree dictionary.
			foreach (PrefabInfo tree in treeReplacements.Keys)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(tree, treeReplacements[tree], removeEntries: false);
			}

			// Iterate through each entry in the master prop dictionary.
			foreach (PrefabInfo prop in propReplacements.Keys)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(prop, propReplacements[prop], removeEntries: false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-building replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Applied replacment tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire building record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static void Revert(PrefabInfo target, PrefabInfo replacement, bool removeEntries = true)
		{
			// List of reverted entries.
			List<KeyValuePair<BuildingInfo, int>> list = new List<KeyValuePair<BuildingInfo, int>>();

			// Iterate through all buildings in the applied all-building replacements dictionary.
			foreach (BuildingInfo building in allBuildingDict.Keys)
			{
				// Iterate through each applied replacement for this building.
				foreach (int index in allBuildingDict[building].Keys)
				{
					// Get currently active replacement and check to see if it matches our reversion parameters (target and replacement match).
					Replacement currentReplacement = allBuildingDict[building][index];
					if (currentReplacement.targetInfo == target && currentReplacement.replacementInfo == replacement)
					{
						// Match - tree or prop?
						if (target is TreeInfo)
						{
							// Tree - revert to original.
							building.m_props[index].m_finalTree = (TreeInfo)target;
						}
						else
						{
							// Prop - revert to original.
							building.m_props[index].m_finalProp = (PropInfo)target;
						}

						// Add this to our list of reverted entries.
						list.Add(new KeyValuePair<BuildingInfo, int>(building, index));
					}
				}
			}

			// If we're not removing entries from the dictionaries, we're done here; return.
			if (!removeEntries)
			{
				return;
			}

			// Remove reverted entries from our dictionary of replacements applied to buildings.
			foreach (KeyValuePair<BuildingInfo, int> item in list)
			{
				RemoveEntry(item.Key, item.Value);
			}

			// Remove entry from our master dictionary of tree/prop replacements.
			if (target is TreeInfo)
			{
				// Tree.
				treeReplacements.Remove(target);
			}
			else
			{
				// Prop.
				propReplacements.Remove(target);
			}
		}


		/// <summary>
		/// Applies a new (or updated) all-building replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Replacment tree/prop prefab</param>
		internal static void Apply(PrefabInfo target, PrefabInfo replacement)
		{
			// Set our initial targeted prefab to the provided target. 
			PrefabInfo targetedPrefab = target;

			// Make sure that target and replacement are the same type before doing anything.
			if ((target == null || replacement == null) || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Tree or prop?
			if (target is TreeInfo)
			{
				// Tree - see if we already have a replacement for this tree.
				if (treeReplacements.ContainsKey(target))
				{
					// We currently have a replacement - change the targeted prefab to replace to match the currently active replacement.
					targetedPrefab = treeReplacements[target];

					// Update dictionary with this replacement.
					treeReplacements[target] = replacement;
				}
				else
				{
					// No current replacement - add this one to the dictionary (retaining the default targeted prefab).
					treeReplacements.Add(target, replacement);
				}
			}
			else
			{
				// Prop - see if we already have a replacement for this prop.
				if (propReplacements.ContainsKey(target))
				{
					// We currently have a replacement - change the targeted prefab to replace to match the currently active replacement.
					targetedPrefab = propReplacements[target];

					// Update dictionary with this replacement.
					propReplacements[target] = replacement;
				}
				else
				{
					// No current replacement - add this one to the dictionary (retaining the default targeted prefab).
					propReplacements.Add(target, replacement);
				}
			}

			// Iterate through each loaded building to apply replacements.
			for (int i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				BuildingInfo loaded = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);

				// Skip any buildings without props.
				if (loaded?.m_props == null)
				{
					continue;
				}

				// Iterate through each building prop in this building.
				for (int j = 0; j < loaded.m_props.Length; ++j)
				{
					BuildingInfo.Prop prop = loaded.m_props[j];
					PrefabInfo originalPrefab = null;

					// Check for any currently active building replacement.
					if (BuildingReplacement.GetOriginal(loaded, j) != null)
                    {
						// Active building replacement; skip this one.
						continue;
                    }

					// Tree or prop?
					if (targetedPrefab is TreeInfo)
					{
						// Tree - check for a match with our designated target.
						if (prop?.m_finalTree != null && prop.m_finalTree == targetedPrefab)
						{
							// Match!  Store the current prefab as the original, and then replace.
							originalPrefab = prop.m_finalTree;
							prop.m_finalTree = (TreeInfo)replacement;
						}
					}
					else if (prop?.m_finalProp != null && prop.m_finalProp == targetedPrefab)
					{
						// Match!  Store the current prefab as the original, and then replace.
						originalPrefab = prop.m_finalProp;
						prop.m_finalProp = (PropInfo)replacement;
					}

					// Check to see if we made a replacement (originalPrefab has been set to a non-null value).
					if (originalPrefab != null)
					{
						// We did - add it to our dictionary of currently active replacements.
						AddEntry(loaded, originalPrefab, replacement, j);
					}
				}
			}
		}


		/// <summary>
		/// Restores a all-building replacement, if any (e.g. after a building replacement has been reverted).
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="index">Prop index</param>
		internal static void Restore(BuildingInfo buildingPrefab, int index)
		{
			// Get current prop.
			BuildingInfo.Prop prop = buildingPrefab.m_props[index];

			PrefabInfo original = null;
			PrefabInfo replacement = null;

			// Does this building prop record contain a tree?
			if (prop.m_finalTree != null)
			{
				// It does - check for active all-building replacement for this tree.
				if (treeReplacements.ContainsKey(prop.m_finalTree))
				{
					// Found an active replacement - apply it.
					original = prop.m_finalTree;
					replacement = treeReplacements[original];
					prop.m_finalTree = (TreeInfo)replacement;
				}
			}
			// Otherwise, does it contain a prop?
			else if (prop.m_finalProp != null)
			{
				// It does - check for active all-building replacement for this prop.
				if (propReplacements.ContainsKey(prop.m_finalProp))
				{
					// Found an active replacement - apply it.
					original = prop.m_finalProp;
					replacement = propReplacements[original];
					prop.m_finalProp = (PropInfo)replacement;
				}
			}

			// If we made a replacement (original has been set to a non-null value), add it to our dictionary of replacements applied to buildings.
			if (original != null)
			{
				AddEntry(buildingPrefab, original, replacement, index);
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-building replacement applied to the given building prop index.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab to check</param>
		/// <param name="index">Prop index to check</param>
		/// <returns>Original prefab if a all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal static PrefabInfo ActiveReplacement(BuildingInfo buildingPrefab, int index)
		{
			// Try to find an entry for this index of this building and index in the master dictionary.
			if (allBuildingDict.ContainsKey(buildingPrefab) && allBuildingDict[buildingPrefab].ContainsKey(index))
			{
				// Entry found - return the stored original prefab.
				return allBuildingDict[buildingPrefab][index].targetInfo;
			}

			// No entry found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Removes an entry from the master dictionary of all-building replacements currently applied to buildings.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="index">Prop index</param>
		internal static void RemoveEntry(BuildingInfo buildingPrefab, int index)
		{
			// Check to see if we have an entry for this building.
			if (allBuildingDict.ContainsKey(buildingPrefab))
			{
				// Yes - remove the given index.
				allBuildingDict[buildingPrefab].Remove(index);

				// Check to see if there are any remaining replacements for this building prefab.
				if (allBuildingDict[buildingPrefab].Count == 0)
				{
					// No remaining replacements - remove the entire building prefab entry.
					allBuildingDict.Remove(buildingPrefab);
				}
			}
		}


		/// <summary>
		/// Adds an entry to the master dictionary of all-building replacements currently applied to buildings.
		/// </summary>
		/// <param name="buildingPrefab">Building prefab</param>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Replacment tree/prop prefab</param>
		/// <param name="index">Prop index</param>
		private static void AddEntry(BuildingInfo buildingPrefab, PrefabInfo target, PrefabInfo replacement, int index)
		{
			// Check to see if we don't already have an entry for this building prefab in the master dictionary.
			if (!allBuildingDict.ContainsKey(buildingPrefab))
			{
				// No existing entry, so add one.
				allBuildingDict.Add(buildingPrefab, new SortedList<int, Replacement>());
			}

			// Check to see if we already have an entry for this index in the master dictionary.
			if (allBuildingDict[buildingPrefab].ContainsKey(index))
			{
				// An entry already exists - just update the replacement info.
				allBuildingDict[buildingPrefab][index].replacementInfo = replacement;
			}
			else
			{
                // No existing entry - create one.
                Replacement newReplacement = new Replacement
                {
                    targetInfo = target,
                    replacementInfo = replacement
                };
                allBuildingDict[buildingPrefab].Add(index, newReplacement);
			}
		}
	}
}