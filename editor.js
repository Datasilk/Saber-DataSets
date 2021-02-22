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
                        //finally, create new dataset and load tab for new dataset
                        S.ajax.post('Datasets/Create', data,
                            function (response) {
                                //load new data set into tab
                                S.popup.hide();
                                S.editor.datasets.records.show(response, partial, name);
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
        load: function (callback, err) {
            //get list of data sets and display in menu
            S.ajax.post('DataSets/GetList', {}, callback, err, true);
        },

        open: function (item) {
            S.editor.datasets.records.show(item.datasetId, item.partialview, item.label);
        }
    },

    records: {
        show: function (id, partial, name, lang, search, start, length) {
            $('.editor .sections > .tab').addClass('hide');
            if (!lang) { lang = $('.tab-toolbar .lang').val(); }
            if (!lang) { lang = 'en'; }
            if (!search) { search = $('.tab-toolbar .search-dataset').val(); }
            if (!start) { start = 1; }
            if (!length) { length = 50; }

            function focusTab() {
                //select tab & generate toolbar
                $('.tab.dataset-' + id + '-section').removeClass('hide');
                S.editor.filebar.update(name, 'icon-dataset', $('.tab.dataset-' + id + '-section .temp-toolbar').html());
                var txtsearch = $('.tab-toolbar .search-dataset');
                txtsearch.val(search);
                txtsearch.on('keyup', (e) => {
                    if (event.key === "Enter") {
                        search = txtsearch.val();
                        S.editor.datasets.records.show(id, partial, name, lang, search);
                    }
                });

                S.editor.lang.load('.tab-toolbar .lang', lang, (e) => {
                    //reload records with selected language
                    lang = $('.tab-toolbar .lang').val();
                    S.editor.datasets.records.show(id, partial, name, lang, search);
                });
                $('.file-bar .new-record').on('click', (e) => {
                    //show popup modal with a content field list form
                    S.editor.datasets.records.add.show(id, partial, name);
                });

                $('.tab-toolbar .dataset-menu > .row.hover').on('click', () => {
                    $('.dataset-menu .drop-menu').show();
                    function hideMenu() {
                        $(document.body).off('click', hideMenu);
                        $('.dataset-menu .drop-menu').hide();
                    }
                    $(document.body).on('click', hideMenu);
                });

                $('.dataset-menu .edit-partial').on('click', () => {
                    S.editor.explorer.open('Content/' + partial);
                });
            }

            if ($('.tab.dataset-' + id + '-section').length == 0) {
                //create new content section
                $('.sections').append('<div class="tab dataset dataset-' + id + '-section"><div class="scroller"></div></div>');
                S.editor.resize.window();

                //create tab
                S.editor.tabs.create('Dataset: ' + name, 'dataset-' + id + '-section', { removeOnClose: true },
                    () => { //onfocus
                        focusTab();
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

            //reload tab contents no matter what
            S.ajax.post('DataSets/Details', { datasetId: id, lang: lang, search: search, start: start, length: length },
                function (d) {
                    $('.tab.dataset-' + id + '-section .scroller').html(d);
                    if ($('.tab-toolbar .lang').children().length == 0) {
                        focusTab();
                    }
                    //add event listeners to each record
                    var dropmenu = $('.tab.dataset-' + id + '-section .temp-row-menu').html();
                    var menus = $('.tab.dataset-' + id + '-section .record-menu');
                    menus.on('click', (e) => {
                        //show drop-down menu for record
                        hideMenus();
                        var target = $(e.target);
                        if (!target.hasClass('record-menu')) {
                            target = target.parents('.record-menu').first();
                        }
                        var recordId = target.attr('data-id');
                        target.append(dropmenu.replace('##edit-record##', Object.keys(S.editor.lang.supported).map((key) => {
                            //add menu items to edit in all supported languages
                            var name = S.editor.lang.supported[key];
                            return '<li><div class="row hover item edit-record-lang" data-lang="' + key + '"><div class="col icon"><svg viewBox="0 0 32 32"><use xlink:href="#icon-edit" x="0" y="0" width="32" height="32"></use></svg></div><div class="col text">Edit in ' + name + '</div></div></li>';
                        }).join('')));
                        $('.edit-record-lang').on('click', (e) => {
                            //edit record based on selected language in menu
                            var target = $(e.target).parents('li').find('.edit-record-lang');
                            var recordlang = target.attr('data-lang');
                            S.editor.datasets.records.edit(id, partial, recordId, name, recordlang);
                        });
                        $(document.body).on('click', hideMenus);
                    });


                    $('.tab.dataset-' + id + '-section tbody tr td:not(:last-child)').on('click', (e) => {
                        //click on row to edit record using selected language
                        var target = $(e.target);
                        if (!e.target.tagName.toLowerCase() != 'tr') {
                            target = target.parents('tr').first();
                        }
                        var recordId = target.attr('data-id');
                        S.editor.datasets.records.edit(id, partial, recordId, name, lang);
                    });

                    function hideMenus() {
                        menus.find('.drop-menu').remove();
                        $(document.body).off('click', hideMenus);
                    }
                }
            );
        },

        add: {
            show: function (id, partial, name) {
                //show content fields form to create new row within data set
                var lang = $('.tab-toolbar .lang').val();
                var popup = S.editor.fields.popup(partial, lang, 'Create a new Record for "' + name + '"', null, 'Create Record', (e, fields) => {
                    //pass content field data to dataset service
                    popup.find('button.apply').hide();
                    S.ajax.post('Datasets/CreateRecord', { datasetId: id, lang: lang, fields: fields }, (response) => {
                        S.popup.hide();
                        //reload records tab
                        S.editor.datasets.records.show(id, partial, name);
                    }, (err) => {
                            S.editor.message('.popup .msg', err.responseText, 'error');
                            popup.find('button.apply').show();
                    });
                });
            }
        },

        edit: function (datasetId, partial, recordId, name, lang) {
            //first, get fields for dataset record based on selected language
            S.ajax.post('Datasets/GetRecord', { datasetId: datasetId, recordId: recordId, lang: lang}, (fieldslist) => {
                var popup = S.editor.fields.popup(partial, lang, 'Update Record for "' + name + '"', fieldslist, 'Update Record', (e, fields) => {
                    //pass content field data to dataset service
                    popup.find('button.apply').hide();
                    S.ajax.post('Datasets/UpdateRecord', { datasetId: datasetId, recordId: recordId, lang: lang, fields: fields }, (response) => {
                        S.popup.hide();
                        //reload records tab
                        S.editor.datasets.records.show(datasetId, partial, name);
                    }, (err) => {
                        S.editor.message('.popup .msg', err.responseText, 'error');
                        popup.find('button.apply').show();
                    });
                });
            }, (err) => {
                S.editor.message(null, err.responseText, 'error');
            });
            
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
        }, () => {
            //no permission to view data sets
        });
        

        //get a list of data sets that exist for this website to display in the dropdown menu
        S.editor.datasets.menu.load();
    }

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
});