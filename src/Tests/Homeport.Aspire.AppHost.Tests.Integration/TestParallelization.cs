// SPDX-License-Identifier: AGPL-3.0-or-later

// Every test class starts the whole topology. Started side by side they compete
// for Docker and the machine, so they run one after the other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
