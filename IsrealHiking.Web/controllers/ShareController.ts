﻿module IsraelHiking.Controllers {

    export interface IShareScope extends angular.IScope {
        title: string;
        shareAddress: string;
        whatappShareAddress: string;
        facebookShareAddress: string;
        width: number;
        height: number;
        size: string;
        embedText: string;
        updateToken: string;
        isLoading: boolean;
        siteUrlId: string;
        openShare(e: Event);
        updateEmbedText(width: number, height: number): void;
        generateUrl(): void;
        clearShareAddress(): void;
        setSize(size: string): void;
    }

    export class ShareController extends BaseMapController {
        private shareModal;
        private $window: angular.IWindowService;

        constructor($scope: IShareScope,
            $modal,
            $http: angular.IHttpService,
            $window:  angular.IWindowService,
            mapService: Services.MapService,
            layersService: Services.LayersService,
            toastr: Toastr) {
            super(mapService);

            this.$window = $window;
            $scope.title = "";
            $scope.width = 400;
            $scope.height = 300;
            $scope.size = "Small";
            $scope.isLoading = false;

            this.shareModal = $modal({
                title: "Share Your Work",
                templateUrl: "views/modals/shareModal.html",
                show: false,
                scope: $scope
            });

            $scope.clearShareAddress = () => {
                $scope.shareAddress = "";
                $scope.whatappShareAddress = "";
                $scope.facebookShareAddress = "";
                $scope.siteUrlId = "";
            }

            $scope.clearShareAddress();

            $scope.updateEmbedText = (width: number, height: number) => {
                $scope.width = width;
                $scope.height = height;
                $scope.embedText = this.getEmbedText($scope);
            }

            $scope.openShare = (e: Event) => {
                $scope.embedText = this.getEmbedText($scope);
                this.shareModal.$promise.then(this.shareModal.show);
                this.suppressEvents(e);
            }

            $scope.generateUrl = () => {
                $scope.isLoading = true;
                var siteUrl = {
                    Title: $scope.title,
                    JsonData: JSON.stringify(layersService.getData())
                } as Common.SiteUrl;
                $http.post(Common.Urls.urls, siteUrl).success((siteUrlResponse: Common.SiteUrl) => {
                    $scope.siteUrlId = siteUrlResponse.Id;
                    $scope.updateToken = siteUrlResponse.ModifyKey;
                    $scope.shareAddress = `http:${this.getShareAddressWithoutProtocol($scope)}`;
                    let escaped = (<any>$window).encodeURIComponent($scope.shareAddress);
                    $scope.whatappShareAddress = `whatsapp://send?text=${escaped}`;
                    $scope.facebookShareAddress = `http://www.facebook.com/sharer/sharer.php?u=${escaped}`;
                    $scope.embedText = this.getEmbedText($scope);
                }).error(() => {
                    toastr.error("Unable to generate URL, please try again later...");
                }).finally(() => {
                    $scope.isLoading = false;
                });
            }

            $scope.setSize = (size: string) => {
                switch (size) {
                    case "Small":
                        $scope.width = 400;
                        $scope.height = 300;
                        break;
                    case "Medium": 
                        $scope.width = 600;
                        $scope.height = 450;
                        break;
                    case "Large":
                        $scope.width = 800;
                        $scope.height = 600;
                        break;
                }
                $scope.embedText = this.getEmbedText($scope);
            }
        }

        private getEmbedText = ($scope: IShareScope) => {
            var shareAddress = this.getShareAddressWithoutProtocol($scope);
            return `<iframe src='${shareAddress}' width='${$scope.width}' height='${$scope.height}' frameborder='0' scrolling='no'></iframe>`;
        }

        private getShareAddressWithoutProtocol = ($scope: IShareScope) => {
            if ($scope.siteUrlId) {
                return `//${this.$window.location.host}/#/?s=${$scope.siteUrlId}`;
            }
            return this.$window.location.href;
        }
    }
}
