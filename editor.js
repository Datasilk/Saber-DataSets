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
                $('.popup form').on('submit', () => { S.editor.datasets.columns.load($('#dataset_partial').val()); });

                //add event listener for partial view browse button
                $('.popup .btn-browse').on('click', (e) => {
                    //show file select popup for partial view selection
                    S.editor.explorer.select('Select Partial View', 'Content/partials', '.html', (file) => {
                        $(e.target).parents('.select-partial').first().find('input').val(file.replace('Content/', '').replace('content/', ''));
                    });
                });
            });

        },

        submit: function (e) {
            $('.popup button.apply').hide();
            e.preventDefault();
            var data = {
                label: $('#dataset_name').val(),
                partial: $('#dataset_partial').val()
            };
            S.ajax.post('Datasets/Create', data, (response) => {
                S.popup.hide();
                S.editor.datasets.show(response, data.label);
                //show new data set in a tab
            }, null, true);
        }
    },

    columns: {
        load: function (partial) {
            //display popup with list of dataset columns
            S.ajax.post('DataSets/LoadColumns', { partial:partial },
                function (response) {
                    S.popup.show('Configure Data Set', response);
                    $('.popup .save-columns').on('click', (e) => {

                    });
                }
            );
        }
    },

    menu: {
        load: function () {
            //get list of data sets and display in menu
        }
    },

    records: {
        show: function (id, name) {
            $('.editor .sections > .tab').addClass('hide');
            if ($('.tab.dataset-' + id + '-section').length == 0) {
                //create new content section
                $('.sections').append('<div class="tab dataset-' + id + '-section"><div class="scroller"></div></div>');

                S.ajax.post('DataSets/Details', { userId: id },
                    function (d) {
                        $('.tab.user-' + id + ' .scroller').html(d);
                        S.editor.users._loadedUsers.push(id);
                        self.details.updateFilebar(id, email);
                        //add event listeners
                        $('.btn-assign-group').on('click', () => { S.editor.users.security.assign(id); })
                        $('.user-group .btn-delete-group').on('click', (e) => {
                            var groupId = $(e.target).parents('.user-group').attr('data-id');
                            S.editor.users.security.remove(id, groupId);
                        });
                    }
                );

                S.editor.tabs.create('Dataset: ' + name, 'dataset-' + id + '-section', {},
                    () => { //onfocus
                        $('.tab.dataset-' + id + '-section').removeClass('hide');
                    },
                    () => { //onblur 
                    },
                    () => { //onsave 
                    }
                );
            }
        }
    }
};


//create a new top menu item
S.editor.topmenu.add('datasets', 'Data Sets');

S.ajax.post('Datasets/GetPermissions', {}, (response) => {
    var bools = response.split(',').map(a => a == '1');
    var sec = S.editor.datasets.security;
    sec.create = bools[0];
    sec.edit = bools[1];
    sec.delete = bools[2];
    sec.view = bools[3];
    sec.adddata = bools[4];
    S.editor.datasets.security = sec;

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

        //get a list of data sets that exist for this website to display in the dropdown menu
        S.editor.datasets.menu.load();
    }

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
});