//loaded when Saber's editor is loaded

S.editor.datasets = {
    add: {
        show: function () {
            S.ajax.post('Datasets/GetCreateForm', {}, (response) => {
                S.popup.show('Create a new Data Set', response);
            });

        }
    }
};

(function () {
    //create a dropdown menu item under the website menu
    S.editor.dropmenu.add('.menu-bar .menu-item-website > .drop-menu > .menu', 'datasets', 'Data Sets', '#icon-datasets', true);

    //create a new top menu item
    S.editor.topmenu.add('datasets', 'Data Sets');

    //add menu item to create new data set
    S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-create', 'New Dataset', '#icon-add-sm', false, S.editor.datasets.add.show);
    $('.menu-bar .item-dataset-create svg').css({ width: '12px', height: '12px', 'margin-top': '5px' });

    //add empty menu item
    S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-empty', 'No data sets exist yet');
    $('.menu-bar .menu-item-datasets .item-dataset-empty').css({ opacity: 0.4 });
    $('.menu-bar .menu-item-datasets .item-dataset-empty .icon').remove();
    $('.menu-bar .menu-item-datasets .item-dataset-empty .text').css({ 'white-space': 'nowrap' });
    console.log($('.menu-bar .menu-item-datasets .item-dataset-empty .text'));
    //TODO: get a list of data sets that exist for this website to display in the dropdown menu

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
})(); 