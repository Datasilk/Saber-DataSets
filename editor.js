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
                $('.popup form').on('submit', (e) => {
                    var name = $('#dataset_name').val();
                    var description = $('#dataset_description').val();
                    var partial = $('#dataset_partial').val();
                    S.popup.hide();
                    S.editor.datasets.columns.load(e, name, description, partial);
                });

                //add event listener for partial view browse button
                $('.popup .btn-browse').on('click', (e) => {
                    //show file select popup for partial view selection
                    S.editor.explorer.select('Select Partial View', 'Content/partials', '.html', (file) => {
                        $(e.target).parents('.select-partial').first().find('input').val(file.replace('Content/', '').replace('content/', ''));
                    });
                });
            });

        }
    },

    columns: {
        load: function (e, name, description, partial) {
            e.preventDefault();
            //display popup with list of dataset columns
            S.ajax.post('DataSets/LoadColumns', { partial:partial },
                function (response) {
                    S.popup.show('Configure Data Set "' + name + '"', response, { className: 'dataset-columns' });
                    //add event listeners
                    $('.dataset-columns .save-columns').on('click', (e2) => {
                        //create dataset
                        e2.preventDefault();
                        $('.popup button.apply').hide();
                        var data = {
                            name: name,
                            partial: partial,
                            description: description,
                            columns: $('.popup .dataset-column').map((i, a) => {
                                return {
                                    Name: $(a).find('.column-name').val(),
                                    DataType: $(a).find('.column-datatype').val(),
                                    MaxLength: $(a).find('.column-maxlength').val() || '0',
                                    DefaultValue: $(a).find('.column-default').val() || ''
                                };
                            })
                        };
                        S.ajax.post('Datasets/Create', data,
                            function (response) {
                                //load new data set into tab
                                S.popup.hide();
                                S.editor.datasets.records.show(response, name);
                            },
                            function (err) {
                                S.editor.message('.popup .msg', err.responseText, 'error');
                            });
                    });
                    $('.dataset-columns').css({ width: 500 });
                },
                (err) => {
                    S.editor.message('.popup .msg', err.responseText, 'error');
                }
            );
        }
    },

    menu: {
        load: function (callback) {
            //get list of data sets and display in menu
            S.ajax.post('DataSets/GetList', {}, callback, null, true);
        },

        open: function (item) {
            S.editor.datasets.records.show(item.datasetId, item.label);
        }
    },

    records: {
        show: function (id, name) {
            $('.editor .sections > .tab').addClass('hide');
            if ($('.tab.dataset-' + id + '-section').length == 0) {
                //create new content section
                $('.sections').append('<div class="tab dataset-' + id + '-section"><div class="scroller"></div></div>');
                S.editor.resize.window();

                S.ajax.post('DataSets/Details', { datasetId: id },
                    function (d) {
                        $('.tab.dataset-' + id + '-section .scroller').html(d);
                    }
                );

                S.editor.tabs.create('Dataset: ' + name, 'dataset-' + id + '-section', {},
                    () => { //onfocus
                        //select tab & generate toolbar
                        $('.tab.dataset-' + id + '-section').removeClass('hide');
                        S.editor.filebar.update(name, 'icon-dataset', '<button class="button new-record">New Record</button>');
                        $('.file-bar .new-record').on('click', (e) => {
                            //show popup modal with a content field list form
                            S.editor.datasets.records.add(id, name);
                        });
                    },
                    () => { //onblur 
                    },
                    () => { //onsave 
                    }
                );
            } else {
                $('.tab.dataset-' + id + '-section').removeClass('hide');
                S.editor.tabs.select('dataset-' + id + '-section');
            }
        },

        add: {
            show: function (id, name) {
                //show content fields form to create new row within data set

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

        //load datasets list into top menu
        S.editor.datasets.menu.load((items) => {
            if (!items || items.length == 0) {
                //add empty menu item
                S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-empty', 'No data sets exist yet');
                $('.menu-bar .menu-item-datasets .item-dataset-empty').css({ opacity: 0.4 });
                $('.menu-bar .menu-item-datasets .item-dataset-empty .icon').remove();
                $('.menu-bar .menu-item-datasets .item-dataset-empty .text').css({ 'white-space': 'nowrap' });
            } else {
                //generate menu items
                for (let x = 0; x < items.length; x++) {
                    let item = items[x];
                    S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-item', item.label, '#icon-dataset', x == 0, () => { S.editor.datasets.menu.open(item); });
                }
            }
        });
        

        //get a list of data sets that exist for this website to display in the dropdown menu
        S.editor.datasets.menu.load();
    }

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
});