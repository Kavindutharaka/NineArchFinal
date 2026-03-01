(function () {
    'use strict';

    angular
        .module('nineArchApp')
        .controller('MainController', MainController);

    MainController.$inject = ['$scope', '$http', '$timeout', '$q'];

    function MainController($scope, $http, $timeout, $q) {
        var vm = $scope;
        var API = '/api/mater';

        // ===== TAB STATE =====
        vm.activeTab = 'generate';

        // ===== MASTER DATA =====
        vm.locations = [];
        vm.hotels = [];
        vm.vehicles = [];
        vm.itineraryTemplates = [];

        // ===== TOUR DATA =====
        vm.tourData = {
            clientName: '',
            tourTitle: 'Hill Country & City Charms',
            arrivalDate: '',
            departureDate: '',
            hotelCategory: '5 Star',
            mealPlan: 'BB',
            numberOfNights: 0,
            totalDays: 0,
            days: [],
            selectedVehicleId: null,
            numberOfAdults: 2,
            numberOfKids: 0,
            perPersonCost: 0,
            currencyCode: 'USD'
        };

        vm.inclusions = [];
        vm.exclusions = [];
        vm.newInclusion = '';
        vm.newExclusion = '';

        // Package options
        vm.option1 = { title: 'Option 1', cost: 0, hotels: [] };
        vm.option2 = { title: 'Option 2', cost: 0, hotels: [] };
        vm.selectedHotelForOpt1 = null;
        vm.selectedHotelForOpt2 = null;
        vm.selectedDayForOpt1   = null;
        vm.selectedDayForOpt2   = null;
        vm.selectedMealForOpt1  = 'BB';
        vm.selectedMealForOpt2  = 'BB';

        // ===== FORMS STATE =====
        vm.showAddHotelForm = false;
        vm.editingHotelId = null;
        vm.newHotel = resetHotelObj();

        vm.showAddVehicleForm = false;
        vm.editingVehicleId = null;
        vm.newVehicle = resetVehicleObj();

        vm.showAddLocationForm = false;
        vm.editingLocationId = null;
        vm.newLocation = resetLocationObj();

        // ===== UI STATE =====
        vm.isGenerating = false;
        vm.generateError = null;

        // ===== DEFAULT INCLUSIONS/EXCLUSIONS =====
        var defaultInclusions = [
            'Airport pick up',
            'Accommodation with daily breakfast at mentioned hotels',
            'Air-conditioned private vehicle',
            'English-speaking chauffeur for the entire tour',
            'All government taxes',
            'Airport drop off',
            'Complimentary tickets for the cultural dance'
        ];
        var defaultExclusions = [
            'Airfare & Sri Lanka visa fees',
            'Meals not mentioned in the itinerary',
            'Entrance tickets',
            'Tips and personal expenses',
            'Early check-in and late check-out charges',
            'Travel insurance'
        ];

        // ===== INITIALIZATION =====
        activate();

        function activate() {
            vm.inclusions = angular.copy(defaultInclusions);
            vm.exclusions = angular.copy(defaultExclusions);
            loadLocations();
            loadHotels();
            loadVehicles();
            loadItineraryTemplates();
        }

        // ===== DB HELPERS =====
        function spGet(query) {
            return $http.post(API + '/sp', { SysID: query });
        }

        function spExec(query) {
            return $http.post(API + '/spd', { SysID: query });
        }

        function esc(val) {
            if (val === null || val === undefined) return '';
            return String(val).replace(/'/g, "''");
        }

        // ===== LOAD DATA =====
        function decodeXmlEntities(str) {
            return (str || '').replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;/g, "'");
        }

        function loadLocations() {
            spGet('EXEC sp_GetLocations').then(function (r) {
                vm.locations = (r.data || []).map(function (loc) {
                    var raw = decodeXmlEntities(loc.images || loc.Images || '');
                    loc.images = raw ? raw.split('|').filter(Boolean) : [];
                    return loc;
                });
            }).catch(function () {
                // Fallback if DB not ready - use empty array
                vm.locations = [];
            });
        }

        function loadHotels() {
            spGet('EXEC sp_GetHotels').then(function (r) {
                vm.hotels = (r.data || []).map(function (h) {
                    var raw = decodeXmlEntities(h.images || h.Images || '');
                    h.images = raw ? raw.split('|').filter(Boolean) : [];
                    return h;
                });
            }).catch(function () {
                vm.hotels = [];
            });
        }

        function loadVehicles() {
            spGet('EXEC sp_GetVehicles').then(function (r) {
                vm.vehicles = (r.data || []).map(function (v) {
                    var raw = decodeXmlEntities(v.images || v.Images || '');
                    v.images = raw ? raw.split('|').filter(Boolean) : [];
                    return v;
                });
            }).catch(function () {
                vm.vehicles = [];
            });
        }

        function loadItineraryTemplates() {
            spGet('EXEC sp_GetItineraryTemplates').then(function (r) {
                vm.itineraryTemplates = r.data || [];
            }).catch(function () {
                vm.itineraryTemplates = [];
            });
        }

        // ===== DATE & DURATION =====
        vm.calculateDuration = function () {
            if (vm.tourData.arrivalDate && vm.tourData.departureDate) {
                var start = new Date(vm.tourData.arrivalDate);
                var end = new Date(vm.tourData.departureDate);
                var diff = Math.ceil((end - start) / (1000 * 3600 * 24)) + 1;
                if (diff > 0) {
                    vm.tourData.totalDays = diff;
                    vm.tourData.numberOfNights = diff - 1;
                    generateDays();
                } else {
                    vm.tourData.totalDays = 0;
                    vm.tourData.numberOfNights = 0;
                    vm.tourData.days = [];
                }
            }
        };

        // ===== SMART ROUTE LABEL =====
        function computeRouteLabel(dayIndex) {
            var days = vm.tourData.days;
            var day = days[dayIndex];
            var total = days.length;
            var loc = day.hotelLocation ||
                      (day.visitedLocations && day.visitedLocations.length > 0 ? day.visitedLocations[0] : '');
            if (dayIndex === 0) {
                return loc ? 'Arrival / ' + loc : 'Arrival';
            } else if (dayIndex === total - 1 && total > 1) {
                return loc ? loc + ' / Departure' : 'Departure';
            } else {
                var prev = days[dayIndex - 1];
                var prevLoc = prev ? (prev.hotelLocation || (prev.visitedLocations && prev.visitedLocations.length > 0 ? prev.visitedLocations[0] : '')) : '';
                if (prevLoc && loc) return prevLoc + ' \u279C ' + loc;
                if (loc) return loc;
                return '';
            }
        }

        function generateDays() {
            var existing = vm.tourData.days;
            vm.tourData.days = [];
            for (var i = 1; i <= vm.tourData.totalDays; i++) {
                var prev = existing[i - 1] || {};
                vm.tourData.days.push({
                    dayNumber: i,
                    itineraryTemplateId: prev.itineraryTemplateId || null,
                    itineraryDescription: prev.itineraryDescription || '',
                    highlights: prev.highlights || '',
                    locationName: prev.locationName || '',
                    hotelId: prev.hotelId || null,
                    hotelName: prev.hotelName || '',
                    hotelDescription: prev.hotelDescription || '',
                    hotelStarRating: prev.hotelStarRating || '',
                    hotelLocation: prev.hotelLocation || '',
                    meals: prev.meals || 'Breakfast included',
                    visitedLocations: prev.visitedLocations || [],
                    selectedLocationForDay: ''
                });
            }
        }

        // ===== DAY MANAGEMENT =====
        vm.addDay = function () {
            vm.tourData.days.push({
                dayNumber: vm.tourData.days.length + 1,
                itineraryTemplateId: null,
                itineraryDescription: '',
                highlights: '',
                locationName: '',
                hotelId: null,
                hotelName: '',
                hotelDescription: '',
                hotelStarRating: '',
                hotelLocation: '',
                meals: 'Breakfast included',
                visitedLocations: [],
                selectedLocationForDay: ''
            });
            vm.tourData.totalDays = vm.tourData.days.length;
            vm.tourData.numberOfNights = vm.tourData.days.length - 1;
            var newIdx = vm.tourData.days.length - 1;
            vm.tourData.days[newIdx].locationName = computeRouteLabel(newIdx);
            // Previous last day was "Departure" — recompute it now it's a middle day
            if (newIdx > 0) {
                vm.tourData.days[newIdx - 1].locationName = computeRouteLabel(newIdx - 1);
            }
        };

        vm.removeDay = function (idx) {
            vm.tourData.days.splice(idx, 1);
            for (var i = 0; i < vm.tourData.days.length; i++) {
                vm.tourData.days[i].dayNumber = i + 1;
            }
            vm.tourData.totalDays = vm.tourData.days.length;
            vm.tourData.numberOfNights = Math.max(0, vm.tourData.days.length - 1);
            // Recompute the day that shifted into this slot
            if (idx < vm.tourData.days.length) {
                vm.tourData.days[idx].locationName = computeRouteLabel(idx);
            }
            // Recompute new last day (Departure label)
            var lastIdx = vm.tourData.days.length - 1;
            if (lastIdx >= 0 && lastIdx !== idx) {
                vm.tourData.days[lastIdx].locationName = computeRouteLabel(lastIdx);
            }
        };

        vm.moveDay = function (idx, dir) {
            var ni = idx + dir;
            if (ni < 0 || ni >= vm.tourData.days.length) return;
            var t = vm.tourData.days[idx];
            vm.tourData.days[idx] = vm.tourData.days[ni];
            vm.tourData.days[ni] = t;
            for (var i = 0; i < vm.tourData.days.length; i++) {
                vm.tourData.days[i].dayNumber = i + 1;
            }
            // Recompute labels for both swapped positions + the day after the lower one
            var lo = Math.min(idx, ni), hi = Math.max(idx, ni);
            [lo, hi].forEach(function (i) {
                vm.tourData.days[i].locationName = computeRouteLabel(i);
            });
            if (hi + 1 < vm.tourData.days.length) {
                vm.tourData.days[hi + 1].locationName = computeRouteLabel(hi + 1);
            }
        };

        vm.onItinerarySelected = function (day) {
            if (!day.itineraryTemplateId) return;
            var tmpl = vm.itineraryTemplates.find(function (t) { return t.id == day.itineraryTemplateId; });
            if (tmpl) {
                day.itineraryDescription = tmpl.description;
                day.highlights = tmpl.highlights;
                day.locationName = tmpl.routeName;
            }
        };

        vm.onHotelSelected = function (day) {
            if (!day.hotelId) return;
            var h = vm.hotels.find(function (h) { return h.id == day.hotelId; });
            if (h) {
                day.hotelName = h.name;
                day.hotelDescription = h.description || '';
                day.hotelStarRating = h.starRating || '';
                day.hotelLocation = h.location || '';
                // Auto-set the smart route label for this day
                var idx = vm.tourData.days.indexOf(day);
                day.locationName = computeRouteLabel(idx);
                // Also update next day since its "prev location" just changed
                if (idx + 1 < vm.tourData.days.length) {
                    vm.tourData.days[idx + 1].locationName = computeRouteLabel(idx + 1);
                }
            }
        };

        // ===== TEMPLATE MANAGEMENT =====
        vm.saveAsTemplate = function (day) {
            if (!day.itineraryDescription.trim()) {
                alert('Please add a description before saving as template.');
                return;
            }
            var name = prompt('Save as template — enter a route name:', day.locationName || 'Custom Template');
            if (name === null) return;
            name = name.trim();
            if (!name) { alert('Template name is required.'); return; }
            spExec("EXEC sp_InsertItineraryTemplate @RouteName='" + esc(name) +
                   "', @Description='" + esc(day.itineraryDescription) +
                   "', @Highlights='" + esc(day.highlights || '') + "'")
                .then(function () { loadItineraryTemplates(); });
        };

        vm.deleteTemplate = function (tmpl) {
            if (!confirm('Delete template "' + tmpl.routeName + '"? This cannot be undone.')) return;
            spExec("EXEC sp_DeleteItineraryTemplate @Id=" + tmpl.id)
                .then(function () { loadItineraryTemplates(); });
        };

        vm.addLocationToDay = function (day) {
            if (day.selectedLocationForDay && !day.visitedLocations.includes(day.selectedLocationForDay)) {
                day.visitedLocations.push(day.selectedLocationForDay);
                day.selectedLocationForDay = '';
            }
        };

        vm.removeLocationFromDay = function (day, idx) {
            day.visitedLocations.splice(idx, 1);
        };

        // ===== LOCATION MANAGEMENT =====
        function resetLocationObj() {
            return { name: '', description: '', images: [], newFiles: [], removedImages: [] };
        }

        vm.saveLocation = function () {
            var n = esc(vm.newLocation.name);
            var d = esc(vm.newLocation.description);

            if (!n) { alert('Location name is required'); return; }

            var query;
            if (vm.editingLocationId) {
                query = "EXEC sp_UpdateLocation @Id=" + vm.editingLocationId + ", @Name='" + n + "', @Description='" + d + "'";
            } else {
                query = "EXEC sp_InsertLocation @Name='" + n + "', @Description='" + d + "'";
            }

            spExec(query).then(function (r) {
                var row = r.data && r.data.length > 0 ? r.data[0] : null;
                var id = vm.editingLocationId || (row ? (row.id || row.Id) : null);
                var hasNewFiles = vm.newLocation.newFiles && vm.newLocation.newFiles.length > 0;
                var locName = vm.newLocation.name;
                var locFiles = vm.newLocation.newFiles;
                var removedImgs = vm.newLocation.removedImages || [];
                vm.cancelLocationEdit();

                var deletions = removedImgs.map(function (path) {
                    $http.delete('/api/image/delete?path=' + encodeURIComponent(path));
                    return spExec("DELETE FROM LocationImages WHERE ImagePath='" + esc(path) + "'");
                });

                $q.all(deletions).then(function () {
                    if (hasNewFiles && id) {
                        uploadImages('locations', locName, locFiles, id, 'location');
                    } else {
                        loadLocations();
                    }
                });
            });
        };

        vm.editLocation = function (loc) {
            vm.newLocation = {
                name: loc.name,
                description: loc.description,
                images: angular.copy(loc.images || []),
                newFiles: [],
                removedImages: []
            };
            vm.editingLocationId = loc.id;
            vm.showAddLocationForm = true;
        };

        vm.deleteLocation = function (loc) {
            if (!confirm('Delete location "' + loc.name + '"?')) return;
            spExec("EXEC sp_DeleteLocation @Id=" + loc.id).then(function () {
                loadLocations();
            });
        };

        vm.cancelLocationEdit = function () {
            vm.newLocation = resetLocationObj();
            vm.editingLocationId = null;
            vm.showAddLocationForm = false;
        };

        // ===== HOTEL MANAGEMENT =====
        function resetHotelObj() {
            return { name: '', starRating: '', foodOptions: '', location: '', description: '', images: [], newFiles: [], removedImages: [] };
        }

        vm.saveHotel = function () {
            var h = vm.newHotel;
            if (!h.name) { alert('Hotel name is required'); return; }

            var query;
            if (vm.editingHotelId) {
                query = "EXEC sp_UpdateHotel @Id=" + vm.editingHotelId +
                    ", @Name='" + esc(h.name) + "', @StarRating='" + esc(h.starRating) +
                    "', @FoodOptions='" + esc(h.foodOptions) + "', @Location='" + esc(h.location) +
                    "', @Description='" + esc(h.description) + "'";
            } else {
                query = "EXEC sp_InsertHotel @Name='" + esc(h.name) + "', @StarRating='" + esc(h.starRating) +
                    "', @FoodOptions='" + esc(h.foodOptions) + "', @Location='" + esc(h.location) +
                    "', @Description='" + esc(h.description) + "'";
            }

            spExec(query).then(function (r) {
                var row = r.data && r.data.length > 0 ? r.data[0] : null;
                var id = vm.editingHotelId || (row ? (row.id || row.Id) : null);
                var removedImgs = h.removedImages || [];
                vm.cancelHotelEdit();

                var deletions = removedImgs.map(function (path) {
                    $http.delete('/api/image/delete?path=' + encodeURIComponent(path));
                    return spExec("DELETE FROM HotelImages WHERE ImagePath='" + esc(path) + "'");
                });

                $q.all(deletions).then(function () {
                    if (h.newFiles && h.newFiles.length > 0 && id) {
                        uploadImages('hotels', h.name, h.newFiles, id, 'hotel');
                    } else {
                        loadHotels();
                    }
                });
            });
        };

        vm.editHotel = function (hotel) {
            vm.newHotel = {
                name: hotel.name,
                starRating: hotel.starRating || '',
                foodOptions: hotel.foodOptions || '',
                location: hotel.location || '',
                description: hotel.description || '',
                images: angular.copy(hotel.images || []),
                newFiles: [],
                removedImages: []
            };
            vm.editingHotelId = hotel.id;
            vm.showAddHotelForm = true;
        };

        vm.deleteHotel = function (hotel) {
            if (!confirm('Delete hotel "' + hotel.name + '"?')) return;
            spExec("EXEC sp_DeleteHotel @Id=" + hotel.id).then(function () {
                loadHotels();
            });
        };

        vm.cancelHotelEdit = function () {
            vm.newHotel = resetHotelObj();
            vm.editingHotelId = null;
            vm.showAddHotelForm = false;
        };

        // ===== VEHICLE MANAGEMENT =====
        function resetVehicleObj() {
            return { model: '', seating: '', luggage: '', airCon: 'Yes', images: [], newFiles: [], removedImages: [] };
        }

        vm.saveVehicle = function () {
            var v = vm.newVehicle;
            if (!v.model) { alert('Vehicle model is required'); return; }

            var query;
            if (vm.editingVehicleId) {
                query = "UPDATE Vehicles SET Model='" + esc(v.model) + "', Seating=" + (v.seating || 0) +
                    ", Luggage=" + (v.luggage || 0) + ", AirCon='" + esc(v.airCon) + "' WHERE Id=" + vm.editingVehicleId +
                    "; SELECT " + vm.editingVehicleId + " AS Id";
            } else {
                query = "EXEC sp_InsertVehicle @Model='" + esc(v.model) + "', @Seating=" + (v.seating || 0) +
                    ", @Luggage=" + (v.luggage || 0) + ", @AirCon='" + esc(v.airCon) + "'";
            }

            var vName = v.model;
            var vFiles = (v.newFiles || []).slice(); // shallow copy — preserves File object references
            var removedImgs = v.removedImages || [];

            spExec(query).then(function (r) {
                var row = r.data && r.data.length > 0 ? r.data[0] : null;
                var id = vm.editingVehicleId || (row ? (row.id || row.Id) : null);
                vm.cancelVehicleEdit();

                var deletions = removedImgs.map(function (path) {
                    $http.delete('/api/image/delete?path=' + encodeURIComponent(path));
                    return spExec("DELETE FROM VehicleImages WHERE ImagePath='" + esc(path) + "'");
                });

                $q.all(deletions).then(function () {
                    if (vFiles.length > 0 && id) {
                        uploadImages('vehicles', vName, vFiles, id, 'vehicle');
                    } else {
                        loadVehicles();
                    }
                });
            });
        };

        vm.editVehicle = function (v) {
            vm.newVehicle = {
                model: v.model,
                seating: v.seating,
                luggage: v.luggage,
                airCon: v.airCon || 'Yes',
                images: angular.copy(v.images || []),
                newFiles: [],
                removedImages: []
            };
            vm.editingVehicleId = v.id;
            vm.showAddVehicleForm = true;
        };

        vm.deleteVehicle = function (v) {
            if (!confirm('Delete vehicle "' + v.model + '"?')) return;
            spExec("DELETE FROM Vehicles WHERE Id=" + v.id + "; SELECT " + v.id + " AS DeletedId").then(function () {
                loadVehicles();
            });
        };

        vm.cancelVehicleEdit = function () {
            vm.newVehicle = resetVehicleObj();
            vm.editingVehicleId = null;
            vm.showAddVehicleForm = false;
        };

        // ===== IMAGE UPLOAD =====
        vm.handleImageUpload = function (event, type) {
            var files = event.target.files;
            var target;
            if (type === 'hotel') target = vm.newHotel;
            else if (type === 'location') target = vm.newLocation;
            else if (type === 'vehicle') target = vm.newVehicle;
            else return;

            if (!target.newFiles) target.newFiles = [];

            for (var i = 0; i < files.length; i++) {
                (function (file) {
                    var reader = new FileReader();
                    reader.onload = function (e) {
                        $scope.$apply(function () {
                            target.images.push(e.target.result);
                            target.newFiles.push(file);
                        });
                    };
                    reader.readAsDataURL(file);
                })(files[i]);
            }
        };

        vm.removeImage = function (type, idx) {
            var target = type === 'hotel' ? vm.newHotel : (type === 'vehicle' ? vm.newVehicle : vm.newLocation);
            var img = target.images[idx];

            if (img && img.indexOf('data:') === 0) {
                // It's a newly selected file (base64) — find its position among only the new files
                var newFileIdx = 0;
                for (var i = 0; i < idx; i++) {
                    if (target.images[i] && target.images[i].indexOf('data:') === 0) newFileIdx++;
                }
                if (target.newFiles && newFileIdx < target.newFiles.length) {
                    target.newFiles.splice(newFileIdx, 1);
                }
            } else {
                // It's an existing saved image — mark it for deletion from DB
                if (!target.removedImages) target.removedImages = [];
                target.removedImages.push(img);
            }

            target.images.splice(idx, 1);
        };

        function uploadImages(category, itemName, files, itemId, type) {
            var fd = new FormData();
            for (var i = 0; i < files.length; i++) {
                fd.append('files', files[i]);
            }
            $http.post('/api/image/upload/' + category + '/' + encodeURIComponent(itemName), fd, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (r) {
                var paths = r.data.paths || [];
                var spName = type === 'hotel' ? 'sp_InsertHotelImage' : (type === 'vehicle' ? 'sp_InsertVehicleImage' : 'sp_InsertLocationImage');
                var idField = type === 'hotel' ? '@HotelId' : (type === 'vehicle' ? '@VehicleId' : '@LocationId');
                var promises = paths.map(function (p) {
                    return spExec("EXEC " + spName + " " + idField + "=" + itemId + ", @ImagePath='" + esc(p) + "'");
                });
                $q.all(promises).then(function () {
                    if (type === 'hotel') loadHotels();
                    else if (type === 'vehicle') loadVehicles();
                    else loadLocations();
                });
            });
        }

        // ===== INCLUSIONS/EXCLUSIONS =====
        vm.addInclusion = function () {
            if (vm.newInclusion && vm.newInclusion.trim()) {
                vm.inclusions.push(vm.newInclusion.trim());
                vm.newInclusion = '';
            }
        };
        vm.removeInclusion = function (idx) { vm.inclusions.splice(idx, 1); };

        vm.addExclusion = function () {
            if (vm.newExclusion && vm.newExclusion.trim()) {
                vm.exclusions.push(vm.newExclusion.trim());
                vm.newExclusion = '';
            }
        };
        vm.removeExclusion = function (idx) { vm.exclusions.splice(idx, 1); };

        // ===== PACKAGE OPTIONS =====
        vm.addHotelToOption = function (option, hotelId, dayNumber, mealPlan) {
            if (!hotelId || !dayNumber) return;
            var h = vm.hotels.find(function (x) { return x.id == hotelId; });
            if (!h) return;
            option.hotels.push({
                dayNumber: parseInt(dayNumber),
                hotelName: h.name,
                hotelDescription: h.description || '',
                hotelStarRating: h.starRating || '',
                hotelLocation: h.location || '',
                foodType: mealPlan || h.foodOptions || 'BB',
                hotelImages: angular.copy(h.images || [])
            });
            // Keep sorted by day
            option.hotels.sort(function (a, b) { return a.dayNumber - b.dayNumber; });
        };

        vm.removeHotelFromOption = function (option, idx) {
            option.hotels.splice(idx, 1);
        };

        // ===== VEHICLE HELPER =====
        vm.getSelectedVehicle = function () {
            if (!vm.tourData.selectedVehicleId) return null;
            return vm.vehicles.find(function (v) { return v.id == vm.tourData.selectedVehicleId; });
        };

        // ===== GENERATE PDF =====
        vm.generatePdf = function () {
            if (!vm.tourData.clientName) {
                vm.generateError = 'Please enter a client name.';
                return;
            }
            if (vm.tourData.days.length === 0) {
                vm.generateError = 'Please add at least one day.';
                return;
            }

            vm.generateError = null;
            vm.isGenerating = true;

            // Build day plans with location images
            var dayPlans = vm.tourData.days.map(function (day) {
                // Collect images from visited locations
                var locImages = [];
                (day.visitedLocations || []).forEach(function (locName) {
                    var loc = vm.locations.find(function (l) { return l.name === locName; });
                    if (loc && loc.images) {
                        locImages = locImages.concat(loc.images);
                    }
                });

                // Also get hotel images
                var hImages = [];
                if (day.hotelId) {
                    var hotel = vm.hotels.find(function (h) { return h.id == day.hotelId; });
                    if (hotel && hotel.images) hImages = hotel.images;
                }

                return {
                    dayNumber: day.dayNumber,
                    itineraryDescription: day.itineraryDescription || '',
                    highlights: day.highlights || '',
                    locationName: day.locationName || '',
                    locationImages: locImages,
                    hotelName: day.hotelName || '',
                    hotelDescription: day.hotelDescription || '',
                    hotelStarRating: day.hotelStarRating || '',
                    hotelLocation: day.hotelLocation || '',
                    hotelImages: hImages,
                    meals: day.meals || 'Breakfast included'
                };
            });

            var selVehicle = vm.getSelectedVehicle();
            var payload = {
                clientName: vm.tourData.clientName,
                arrivalDate: vm.tourData.arrivalDate || '',
                departureDate: vm.tourData.departureDate || '',
                tourTitle: vm.tourData.tourTitle || '',
                hotelCategory: vm.tourData.hotelCategory,
                mealPlan: vm.tourData.mealPlan,
                numberOfNights: vm.tourData.numberOfNights || 0,
                days: dayPlans,
                selectedVehicleId: vm.tourData.selectedVehicleId || 0,
                vehicleModel: selVehicle ? (selVehicle.model || '') : '',
                vehicleSeating: selVehicle ? (selVehicle.seating || 0) : 0,
                vehicleAirCon: selVehicle ? (selVehicle.airCon || '') : '',
                vehicleImage: selVehicle && selVehicle.images && selVehicle.images.length > 0 ? selVehicle.images[0] : '',
                numberOfAdults: vm.tourData.numberOfAdults || 1,
                numberOfKids: vm.tourData.numberOfKids || 0,
                perPersonCost: vm.tourData.perPersonCost || 0,
                totalCost: (vm.tourData.perPersonCost || 0) * Math.max(vm.tourData.numberOfAdults || 1, 1),
                currencyCode: vm.tourData.currencyCode || 'USD',
                inclusions: vm.inclusions,
                exclusions: vm.exclusions,
                option1: vm.option1.hotels.length > 0 ? {
                    title: vm.option1.title || 'Option 1',
                    cost: vm.option1.cost || 0,
                    hotels: vm.option1.hotels
                } : null,
                option2: vm.option2.hotels.length > 0 ? {
                    title: vm.option2.title || 'Option 2',
                    cost: vm.option2.cost || 0,
                    hotels: vm.option2.hotels
                } : null
            };

            $http({
                method: 'POST',
                url: '/api/pdf/generate',
                data: payload,
                responseType: 'arraybuffer'
            }).then(function (response) {
                var blob = new Blob([response.data], { type: 'application/pdf' });
                var url = window.URL.createObjectURL(blob);
                var cd = response.headers('Content-Disposition');
                var fileName = '9Arch_TourPackage.pdf';
                if (cd) {
                    var match = cd.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
                    if (match && match[1]) fileName = match[1].replace(/['"]/g, '');
                }
                var a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                document.body.appendChild(a);
                a.click();
                $timeout(function () {
                    document.body.removeChild(a);
                    window.URL.revokeObjectURL(url);
                }, 100);
            }).catch(function (err) {
                vm.generateError = 'Failed to generate PDF. Please check server logs.';
                console.error('PDF error:', err);
            }).finally(function () {
                vm.isGenerating = false;
            });
        };

        // ===== PREVIEW PDF HTML =====
        vm.previewPdf = function () {
            // Opens the PDF HTML in a new window for debugging
            var dayPlans = vm.tourData.days.map(function (day) {
                var locImages = [];
                (day.visitedLocations || []).forEach(function (locName) {
                    var loc = vm.locations.find(function (l) { return l.name === locName; });
                    if (loc && loc.images) locImages = locImages.concat(loc.images);
                });
                var hImages = [];
                if (day.hotelId) {
                    var hotel = vm.hotels.find(function (h) { return h.id == day.hotelId; });
                    if (hotel && hotel.images) hImages = hotel.images;
                }
                return {
                    dayNumber: day.dayNumber,
                    itineraryDescription: day.itineraryDescription || '',
                    highlights: day.highlights || '',
                    locationName: day.locationName || '',
                    locationImages: locImages,
                    hotelName: day.hotelName || '',
                    hotelDescription: day.hotelDescription || '',
                    hotelStarRating: day.hotelStarRating || '',
                    hotelLocation: day.hotelLocation || '',
                    hotelImages: hImages,
                    meals: day.meals || 'Breakfast included'
                };
            });

            var selVehicleP = vm.getSelectedVehicle();
            var payload = {
                clientName: vm.tourData.clientName || 'Preview Client',
                arrivalDate: vm.tourData.arrivalDate || '',
                departureDate: vm.tourData.departureDate || '',
                tourTitle: vm.tourData.tourTitle || '',
                hotelCategory: vm.tourData.hotelCategory,
                mealPlan: vm.tourData.mealPlan,
                numberOfNights: vm.tourData.numberOfNights || 0,
                days: dayPlans,
                selectedVehicleId: vm.tourData.selectedVehicleId || 0,
                vehicleModel: selVehicleP ? (selVehicleP.model || '') : '',
                vehicleSeating: selVehicleP ? (selVehicleP.seating || 0) : 0,
                vehicleAirCon: selVehicleP ? (selVehicleP.airCon || '') : '',
                vehicleImage: selVehicleP && selVehicleP.images && selVehicleP.images.length > 0 ? selVehicleP.images[0] : '',
                numberOfAdults: vm.tourData.numberOfAdults || 1,
                numberOfKids: vm.tourData.numberOfKids || 0,
                perPersonCost: vm.tourData.perPersonCost || 0,
                totalCost: (vm.tourData.perPersonCost || 0) * Math.max(vm.tourData.numberOfAdults || 1, 1),
                currencyCode: vm.tourData.currencyCode || 'USD',
                inclusions: vm.inclusions,
                exclusions: vm.exclusions,
                option1: vm.option1.hotels.length > 0 ? vm.option1 : null,
                option2: vm.option2.hotels.length > 0 ? vm.option2 : null
            };

            $http.post('/api/pdf/preview', payload).then(function (r) {
                var w = window.open('', '_blank');
                w.document.write(r.data);
                w.document.close();
            });
        };
    }
})();
