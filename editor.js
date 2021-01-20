//loaded when Saber's editor is loaded
S.editor.datasets = {
    security: {
        create: false,
        edit: false,
        delete: false,
        view: false,
        adddata: false
    },
    add: {
        show: function () {
            S.ajax.post('Datasets/GetCreateForm', {}, (response) => {
                S.popup.show('Create a new Data Set', response);
                $('.popup form').on('submit', S.editor.datasets.submit);
            });

        },

        submit: function (e) {
            $('.popup button.apply').hide();
            e.preventDefault();
            var data = {
                name: $('#dataset_name').val(),
                partial: $('#dataset_partial').val()
            };
            S.ajax.post('Datasets/Create', data, (response) => {
                S.popup.hide();
            });
        }
    },

    menu: {
        load: function () {
            //get list of data sets and display in menu
        }
    }
};

S.ajax.post('Datasets/GetPermissions', {}, (response) => {
    var bools = response.split(',').map(a => a == '1');
    var sec = S.editor.datasets.security;
    sec.create = bools[0];
    sec.edit = bools[1];
    sec.delete = bools[2];
    sec.view = bools[3];
    sec.adddata = bools[4];
    S.editor.datasets.security = sec;

    //create a new top menu item
    S.editor.topmenu.add('datasets', 'Data Sets');

    if (sec.create == true) {
        //add menu item to create new data set
        S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-create', 'New Dataset', '#icon-add-sm', false, S.editor.datasets.add.show);
        $('.menu-bar .item-dataset-create svg').css({ width: '12px', height: '12px', 'margin-top': '5px' });
    }

    if (sec.view == true) {
        //create a dropdown menu item under the website menu
        S.editor.dropmenu.add('.menu-bar .menu-item-website > .drop-menu > .menu', 'datasets', 'Data Sets', '#icon-datasets', true);

        //add empty menu item
        S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-empty', 'No data sets exist yet');
        $('.menu-bar .menu-item-datasets .item-dataset-empty').css({ opacity: 0.4 });
        $('.menu-bar .menu-item-datasets .item-dataset-empty .icon').remove();
        $('.menu-bar .menu-item-datasets .item-dataset-empty .text').css({ 'white-space': 'nowrap' });
        console.log($('.menu-bar .menu-item-datasets .item-dataset-empty .text'));

        //get a list of data sets that exist for this website to display in the dropdown menu
        S.editor.datasets.load();
    }

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
    }
});