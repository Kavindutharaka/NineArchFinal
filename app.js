// AngularJS Application 
var app = angular.module('tourApp', []);

app.controller('MainController', function ($scope) {
    // Initialize variables
    $scope.activeTab = 'generate';

    // Tour Data
    $scope.tourData = {
        peopleCount: 1,
        startDate: null,
        endDate: null,
        totalDays: 0,
        pdfTitle: '',
        days: [],
        selectedVehicle: ''
    };

    // Hotels
    $scope.hotels = [];
    $scope.newHotel = {
        name: '',
        type: '',
        foodOptions: '',
        location: '',
        images: []
    };
    $scope.showAddHotelForm = false;
    $scope.editingHotelIndex = null;

    // Vehicles
    $scope.vehicles = [];
    $scope.newVehicle = {
        model: '',
        seating: '',
        luggage: '',
        aircon: '',
        images: []
    };
    $scope.showAddVehicleForm = false;
    $scope.editingVehicleIndex = null;

    // Locations
    $scope.locations = [];
    $scope.newLocation = {
        name: '',
        description: ''
    };
    $scope.showAddLocationForm = false;
    $scope.editingLocationIndex = null;

    // Inclusions & Exclusions
    $scope.inclusionsList = [
        { name: 'Airport pick up', selected: true },
        { name: '4 nights accommodation with daily breakfast', selected: true },
        { name: 'Air-conditioned private vehicle', selected: true },
        { name: 'English-speaking chauffeur for the entire tour', selected: true },
        { name: 'All government taxes', selected: true },
        { name: 'Airport drop off', selected: true },
        { name: 'Complementary tickets for the cultural dance', selected: true }
    ];

    $scope.exclusionsList = [
        { name: 'Airfare & Sri Lanka visa fees', selected: true },
        { name: 'Meals not mentioned in the itinerary', selected: true },
        { name: 'Entrance tickets', selected: true },
        { name: 'Tips and personal expenses', selected: true },
        { name: 'Early check in and late check out charges', selected: true },
        { name: 'Travel insurance', selected: true }
    ];

    // Sample data
    $scope.loadSampleData = function () {
        $scope.locations = [
            { name: 'Negombo', description: 'Coastal town with beautiful beaches, Negombo Lagoon boat rides, Dutch Canal, and fresh seafood restaurants.' },
            { name: 'Kandy', description: 'Sacred city featuring Temple of the Tooth Relic, Kandy Lake, Upper Lake viewpoint, and traditional cultural dance performances.' },
            { name: 'Nuwara Eliya', description: 'Hill country paradise with tea plantations, Gregory Lake, Victoria Park, colonial architecture, and cool climate.' },
            { name: 'Colombo', description: 'Vibrant capital city with Galle Face Green, Independence Square, Gangaramaya Temple, modern shopping malls.' },
            { name: 'Pinnawala', description: 'Famous elephant orphanage where you can observe elephants bathing and feeding.' }
        ];

        $scope.hotels = [
            {
                name: 'Vivanta Colombo, Airport Garden',
                type: '5-star',
                foodOptions: 'BB',
                location: 'Negombo',
                images: []
            },
            {
                name: 'The Golden Crown Hotel',
                type: '5-star',
                foodOptions: 'BB',
                location: 'Kandy',
                images: []
            },
            {
                name: 'The Golden Ridge Hotel',
                type: '5-star',
                foodOptions: 'BB',
                location: 'Nuwara Eliya',
                images: []
            },
            {
                name: 'Courtyard by Marriott Colombo',
                type: '5-star',
                foodOptions: 'BB',
                location: 'Colombo',
                images: []
            }
        ];

        $scope.vehicles = [
            {
                model: 'KDH High Roof',
                seating: 9,
                luggage: 10,
                aircon: 'Yes',
                images: []
            }
        ];
    };

    // Load sample data on init
    $scope.loadSampleData();

    // Calculate tour duration
    $scope.calculateDuration = function () {
        if ($scope.tourData.startDate && $scope.tourData.endDate) {
            var start = new Date($scope.tourData.startDate);
            var end = new Date($scope.tourData.endDate);
            var timeDiff = end.getTime() - start.getTime();
            var daysDiff = Math.ceil(timeDiff / (1000 * 3600 * 24)) + 1;

            if (daysDiff > 0) {
                $scope.tourData.totalDays = daysDiff;
                $scope.generateDays();
            } else {
                $scope.tourData.totalDays = 0;
                $scope.tourData.days = [];
            }
        }
    };

    // Generate days array
    $scope.generateDays = function () {
        $scope.tourData.days = [];
        for (var i = 1; i <= $scope.tourData.totalDays; i++) {
            $scope.tourData.days.push({
                dayNumber: i,
                hotel: '',
                locations: [],
                selectedLocation: ''
            });
        }
    };

    // Add location to day
    $scope.addLocationToDay = function (day) {
        if (day.selectedLocation && !day.locations.includes(day.selectedLocation)) {
            day.locations.push(day.selectedLocation);
            day.selectedLocation = '';
        }
    };

    // Remove location from day
    $scope.removeLocationFromDay = function (day, index) {
        day.locations.splice(index, 1);
    };

    // Image upload handler
    $scope.handleImageUpload = function (event, type) {
        var files = event.target.files;
        var target = type === 'hotel' ? $scope.newHotel : $scope.newVehicle;

        for (var i = 0; i < files.length; i++) {
            var reader = new FileReader();
            reader.onload = (function (file) {
                return function (e) {
                    $scope.$apply(function () {
                        target.images.push(e.target.result);
                    });
                };
            })(files[i]);
            reader.readAsDataURL(files[i]);
        }
    };

    // Remove image
    $scope.removeImage = function (type, index) {
        var target = type === 'hotel' ? $scope.newHotel : $scope.newVehicle;
        target.images.splice(index, 1);
    };

    // Hotel functions
    $scope.saveHotel = function () {
        if ($scope.editingHotelIndex !== null) {
            $scope.hotels[$scope.editingHotelIndex] = angular.copy($scope.newHotel);
            $scope.editingHotelIndex = null;
        } else {
            $scope.hotels.push(angular.copy($scope.newHotel));
        }
        $scope.resetHotelForm();
    };

    $scope.editHotel = function (index) {
        $scope.newHotel = angular.copy($scope.hotels[index]);
        $scope.editingHotelIndex = index;
        $scope.showAddHotelForm = true;
    };

    $scope.deleteHotel = function (index) {
        if (confirm('Are you sure you want to delete this hotel?')) {
            $scope.hotels.splice(index, 1);
        }
    };

    $scope.cancelHotelEdit = function () {
        $scope.resetHotelForm();
    };

    $scope.resetHotelForm = function () {
        $scope.newHotel = {
            name: '',
            type: '',
            foodOptions: '',
            location: '',
            images: []
        };
        $scope.showAddHotelForm = false;
        $scope.editingHotelIndex = null;
    };

    // Vehicle functions
    $scope.saveVehicle = function () {
        if ($scope.editingVehicleIndex !== null) {
            $scope.vehicles[$scope.editingVehicleIndex] = angular.copy($scope.newVehicle);
            $scope.editingVehicleIndex = null;
        } else {
            $scope.vehicles.push(angular.copy($scope.newVehicle));
        }
        $scope.resetVehicleForm();
    };

    $scope.editVehicle = function (index) {
        $scope.newVehicle = angular.copy($scope.vehicles[index]);
        $scope.editingVehicleIndex = index;
        $scope.showAddVehicleForm = true;
    };

    $scope.deleteVehicle = function (index) {
        if (confirm('Are you sure you want to delete this vehicle?')) {
            $scope.vehicles.splice(index, 1);
        }
    };

    $scope.cancelVehicleEdit = function () {
        $scope.resetVehicleForm();
    };

    $scope.resetVehicleForm = function () {
        $scope.newVehicle = {
            model: '',
            seating: '',
            luggage: '',
            aircon: '',
            images: []
        };
        $scope.showAddVehicleForm = false;
        $scope.editingVehicleIndex = null;
    };

    // Location functions
    $scope.saveLocation = function () {
        if ($scope.editingLocationIndex !== null) {
            $scope.locations[$scope.editingLocationIndex] = angular.copy($scope.newLocation);
            $scope.editingLocationIndex = null;
        } else {
            $scope.locations.push(angular.copy($scope.newLocation));
        }
        $scope.resetLocationForm();
    };

    $scope.editLocation = function (index) {
        $scope.newLocation = angular.copy($scope.locations[index]);
        $scope.editingLocationIndex = index;
        $scope.showAddLocationForm = true;
    };

    $scope.deleteLocation = function (index) {
        if (confirm('Are you sure you want to delete this location?')) {
            $scope.locations.splice(index, 1);
        }
    };

    $scope.cancelLocationEdit = function () {
        $scope.resetLocationForm();
    };

    $scope.resetLocationForm = function () {
        $scope.newLocation = {
            name: '',
            description: ''
        };
        $scope.showAddLocationForm = false;
        $scope.editingLocationIndex = null;
    };

    // Generate PDF
    $scope.generatePDF = function () {
        // Validate required fields
        if (!$scope.tourData.startDate || !$scope.tourData.endDate) {
            alert('Please select start and end dates');
            return;
        }

        if (!$scope.tourData.pdfTitle) {
            alert('Please select a PDF title');
            return;
        }

        // Prepare data for backend
        var pdfData = {
            tourInfo: $scope.tourData,
            selectedInclusions: $scope.inclusionsList.filter(function (item) { return item.selected; }),
            selectedExclusions: $scope.exclusionsList.filter(function (item) { return item.selected; }),
            vehicleDetails: $scope.vehicles.find(function (v) { return v.model === $scope.tourData.selectedVehicle; })
        };

        console.log('PDF Data to be sent to backend:', pdfData);

        // This would be replaced with actual API call
        alert('PDF generation request prepared!\n\nData logged to console.\n\nIn production, this would call:\nPOST /api/pdf/generate');

        // Example API call (uncomment when backend is ready):
        /*
        fetch('/api/pdf/generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(pdfData)
        })
        .then(response => response.blob())
        .then(blob => {
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = 'tour-package.pdf';
            a.click();
        })
        .catch(error => console.error('Error:', error));
        */
    };
});