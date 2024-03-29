﻿//loaded when Saber's editor is loaded
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
            //display form to add new dataset
            S.ajax.post('Datasets/GetCreateForm', {}, (response) => {
                S.popup.show('Create a new Data Set', response);
                $('.popup form').on('submit', (e) => {
                    var name = $('#dataset_name').val();
                    var description = $('#dataset_description').val();
                    var partial = $('#dataset_partial').val();
                    var type = $('#dataset_type').val();
                    S.editor.datasets.columns.load(e, name, description, partial, type);
                });

                //add event listener for partial view browse button
                $('.popup .btn-browse').on('click', (e) => {
                    //show file select popup for partial view selection
                    S.editor.explorer.select('Select Partial View', 'Content/partials', '.html', (file) => {
                        $(e.target).parents('.select-partial').first().find('input').val(file.replace('Content/', '').replace('content/', ''));
                    });
                });
            });
        },
        finish: function (name, description, partial, type) {
            //finish adding new dataset
            var data = {
                name: name,
                partial: partial,
                description: description,
                type: type,
                columns: $('.popup .dataset-column').map((i, a) => {
                    return {
                        Name: $(a).find('.column-name').val(),
                        DataType: $(a).find('.column-datatype').val(),
                        MaxLength: $(a).find('.column-maxlength').val() || '0',
                        DefaultValue: $(a).find('.column-default').val() || '',
                        Dataset: $(a).find('.column-dataset').val() || '',
                        ColumnName: $(a).find('.column-colname').val() || '',
                        ListType: $(a).find('.column-listtype').val() || '',
                    };
                })
            };
            S.ajax.post('Datasets/Create', data,
                function (response) {
                    //load new data set into tab
                    S.popup.hide();
                    S.editor.datasets.menu.load(() => {
                        S.editor.datasets.records.show(response, partial, name);
                    });
                    
                },
                function (err) {
                    S.editor.error('.popup .msg', err.responseText);
                });
        }
    },

    columns: {
        load: function (e, name, description, partial, type) {
            e.preventDefault();
            //display popup with list of dataset columns
            S.ajax.post('DataSets/LoadColumns', { partial:partial },
                function (response) {
                    S.popup.hide();
                    S.popup.show('Configure Data Set "' + name + '"', response, { className: 'dataset-columns' });
                    //add event listeners
                    S.editor.datasets.columns.initForm(partial);
                    $('.dataset-columns .save-columns').on('click', (e2) => {
                        //create dataset
                        e2.preventDefault();
                        $('.popup button.apply').hide();
                        //finally, create new dataset and load tab for new dataset
                        S.editor.datasets.add.finish(name, description, partial, type);
                    });
                    $('.dataset-columns').css({ width: 500 });
                },
                (err) => {
                    S.editor.error('.popup .msg', err.responseText);
                }
            );
        },

        initForm: function (partial) {
            $('.popup .column-datatype').on('input', (e) => {
                var target = $(e.target);
                var parent = target.parents('.dataset-column').first();
                if (target.val() == 'relationship') {
                    parent.find('.column-details').hide();
                    parent.find('.column-relationship').show();
                } else if (target.val() == 'relationship-id') {
                    parent.find('.column-details').hide();
                } else {
                    parent.find('.column-details').show();
                    parent.find('.column-relationship').hide();
                }
            });

            $('.popup .column-dataset').on('input', (e) => {
                var target = $(e.target);
                var parent = target.parents('.dataset-column').first();
                var data = { datasetId: target.val() };
                S.ajax.post('DataSets/RelationalColumns', data, (response) => {
                    var list = JSON.parse(response);
                    var options = '';
                    for (var x = 0; x < list.length; x++) {
                        var item = list[x];
                        options += '<option value="' + item + '">' + item + '</option>';
                    }
                    var listtype = parent.find('.column-listtype');
                    var colname = parent.find('.column-colname');
                    colname.html(options);
                    listtype.on('input', () => {
                        if (listtype.val() == '2') {
                            parent.find('.related-column').show();
                        } else {
                            parent.find('.related-column').hide();
                        }
                    })

                });
            });
        },

        checkPartial: function (datasetId, partial, name) {
            S.ajax.post('DataSets/LoadNewColumns', { datasetId: datasetId }, (response) => {
                if (response == 'success') {
                    S.editor.message('', 'Successfully checked partial view for new data set columns and found no new columns.');
                } else {
                    //display popup with list of dataset columns
                    S.popup.show('Update Data Set "' + name + '"', response, { className: 'dataset-columns' });
                    //add event listeners
                    S.editor.datasets.columns.initForm(partial);
                    $('.dataset-columns .save-columns').on('click', (e) => {
                        //create dataset
                        e.preventDefault();
                        $('.popup button.apply').hide();
                        //finally, update dataset with new columns
                        var data = {
                            datasetId: datasetId,
                            columns: $('.popup .dataset-column').map((i, a) => {
                                return {
                                    Name: $(a).find('.column-name').val(),
                                    DataType: $(a).find('.column-datatype').val(),
                                    MaxLength: $(a).find('.column-maxlength').val() || '0',
                                    DefaultValue: $(a).find('.column-default').val() || '',
                                    Dataset: $(a).find('.column-dataset').val() || '',
                                    ColumnName: $(a).find('.column-colname').val() || '',
                                    ListType: $(a).find('.column-listtype').val() || '',
                                };
                            })
                        };
                        S.ajax.post('Datasets/UpdateColumns', data,
                            function () {
                                //load new data set into tab
                                S.popup.hide();
                                S.editor.datasets.menu.load(() => {
                                    S.editor.datasets.records.show(datasetId, partial, name);
                                });
                                S.editor.message('', 'Successfully updated data set with new columns.');
                            },
                            function (err) {
                                S.editor.error('.popup .msg', err.responseText);
                            });
                    });
                    $('.dataset-columns').css({ width: 500 });
                }
            }, (err) => {
                S.editor.error('', err.responseText);
            });
        }
    },

    menu: {
        load: function (callback) {
            //get list of data sets and display in menu
            S.ajax.post('DataSets/GetList', { owned: true, all: true }, (items) => {
                $('.menu-bar .menu-item-datasets li.is-dataset').remove();
                if (!items || items.length == 0) {
                    //add empty menu item
                    S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-empty', 'No data sets exist yet', null, null, null, 'is-dataset');
                    $('.menu-bar .menu-item-datasets .item-dataset-empty').css({ opacity: 0.4 });
                    $('.menu-bar .menu-item-datasets .item-dataset-empty .icon').remove();
                    $('.menu-bar .menu-item-datasets .item-dataset-empty .text').css({ 'white-space': 'nowrap' });
                } else {
                    //generate menu items
                    for (let x = 0; x < items.length; x++) {
                        let item = items[x];
                        S.editor.dropmenu.add('.menu-bar .menu-item-datasets > .drop-menu > .menu', 'dataset-item dataset-' + item.datasetId, item.label, '#icon-dataset', x == 0, () => { S.editor.datasets.menu.open(item); }, 'is-dataset');
                    }
                }
                if (callback) { callback(); }
            }, () => {
                //no permission to view data sets
            }, true);
        },

        open: function (item) {
            S.editor.datasets.records.show(item.datasetId, item.partialview, item.label);
        }
    },

    records: {
        excluded: ['id', 'lang', 'datecreated', 'datemodified'],
        show: function (id, partial, name, lang, start, length) {
            $('.editor .sections > .tab').addClass('hide');
            if (!lang) { lang = $('.tab-toolbar .lang').val(); }
            if (!lang) { lang = 'en'; }
            var search = $('.tab-toolbar .search-dataset').val(); 
            if (!start) { start = 1; }
            if (!length) { length = 20; }
            var filters = [];
            var sort = [];

            function focusTab() {
                //select tab & generate toolbar
                S.editor.filebar.buttons.hide();
                $('.tab.dataset-' + id + '-section').removeClass('hide');
                S.editor.filebar.update(name, 'icon-dataset', $('.tab.dataset-' + id + '-section .temp-toolbar').html());
                var txtsearch = $('.tab-toolbar .search-dataset');
                txtsearch.val(search);
                txtsearch.on('keyup', (e) => {
                    if (e.key === "Enter") {
                        search = txtsearch.val();
                        S.editor.datasets.records.show(id, partial, name, lang);
                    }
                });

                S.editor.lang.load('.tab-toolbar .lang', lang, (e) => {
                    //reload records with selected language
                    lang = $('.tab-toolbar .lang').val();
                    S.editor.datasets.records.show(id, partial, name, lang);
                });
                $('.file-bar .new-record').on('click', (e) => {
                    //show popup modal with a content field list form
                    S.editor.datasets.records.add.show(id, partial, name);
                });

                $('.tab-toolbar .dataset-menu > .row.hover').on('click', () => {
                    //show drop-down menu for data sets
                    $('.dataset-menu .drop-menu').show();
                    function hideMenu() {
                        $(document.body).off('click', hideMenu);
                        $('.dataset-menu .drop-menu').hide();
                    }
                    $(document.body).on('click', hideMenu);
                });

                $('.dataset-menu .edit-partial').on('click', () => {
                    //load associated partial view in a new tab
                    S.editor.explorer.open('Content/' + partial);
                });

                $('.dataset-menu .update-partial').on('click', () => {
                    //check partial view for changes
                    S.editor.datasets.columns.checkPartial(id, partial, name);
                });

                $('.dataset-menu .edit-info').on('click', () => {
                    S.ajax.post('Datasets/GetUpdateInfoForm', {datasetId: id}, (response) => {
                        var popup = S.popup.show('Update an existing Data Set', response);
                        $('.popup form').on('submit', (e) => {
                            //update dataset info
                            e.preventDefault();
                            popup.find('button.apply').hide();
                            var newname = $('#dataset_name').val();
                            var description = $('#dataset_description').val();
                            var type = $('#dataset_type').val();
                            S.ajax.post('Datasets/UpdateInfo', { datasetId: id, name: newname, description: description, type: type}, (response) => {
                                S.popup.hide();
                                //change datasets dropdown menu item, tab title & toolbar title with updated dataset name
                                name = newname;
                                $('.file-bar .file-path').html(newname);
                                $('.tab-dataset-' + id + '-section .tab-title').html('Dataset: ' + newname);
                                S.editor.datasets.menu.load();
                            }, (err) => {
                                S.editor.error('.popup .msg', err.responseText);
                                popup.find('button.apply').show();
                            });
                        });
                    });
                });

                $('.dataset-menu .delete-dataset').on('click', () => {
                    //user requests to delete dataset
                    S.editor.message.confirm('Delete Dataset', 'Do you really want to delete the Data Set "' + name + '"? This cannot be undone and you will lose all data within the Data Set permanently.', { type: 'okaycancel' }, (choice) => {
                        S.popup.hide();
                        if (choice === true) {
                            //close selected tab
                            var tabid = 'dataset-' + id + '-section';
                            S.editor.tabs.close(tabid, tabid, () => { });
                            //delete dataset
                            S.ajax.post('Datasets/Delete', { datasetId: id }, (response) => {
                                //update Data Sets top menu
                                S.editor.datasets.menu.load();
                            }, (err) => {
                                S.editor.error('.popup .msg', err.responseText);
                            });
                        }
                    });
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
            S.ajax.post('DataSets/Details', { datasetId: id, lang: lang, start: start, length: length, filters: filters, sort: sort },
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
                        $('.drop-menu .delete-record').on('click', (e) => {
                            if (confirm("Do you really want to delete this record? All related records in all relationship tables will also be deleted. This cannot be undone!") == true) {
                                S.editor.datasets.records.delete(id, recordId, partial, name);
                            }
                        });
                        $(document.body).on('click', hideMenus);
                    });


                    $('.tab.dataset-' + id + '-section tbody tr td:not(.no-details)').on('click', (e) => {
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
                        S.editor.datasets.records.show(id, partial, name, lang);
                    }, (err) => {
                            S.editor.error('.popup .msg', err.responseText);
                            popup.find('button.apply').show();
                    });
                }, S.editor.datasets.records.excluded, 'DataSets/RenderContentFields?datasetId=' + id);
            }
        },

        edit: function (datasetId, partial, recordId, name, lang) {
            //first, get fields for dataset record based on selected language
            S.ajax.post('Datasets/GetRecord', { datasetId: datasetId, recordId: recordId, lang: lang}, (fieldslist) => {
                var popup = S.editor.fields.popup(partial, lang, 'Update Record for "' + name + '"', fieldslist, 'Update Record', (e, fields) => {
                    //pass content field data to dataset service
                    popup.find('button.apply').hide();
                    //update record in database
                    S.ajax.post('Datasets/UpdateRecord', { datasetId: datasetId, recordId: recordId, lang: lang, fields: fields }, (response) => {
                        S.popup.hide();
                        //reload records tab
                        S.editor.datasets.records.show(datasetId, partial, name, lang);
                    }, (err) => {
                        S.editor.error('.popup .msg', err.responseText);
                        popup.find('button.apply').show();
                    });
                }, S.editor.datasets.records.excluded, 'DataSets/RenderContentFields?datasetId=' + datasetId + '&recordId=' + recordId);
            }, (err) => {
                S.editor.error('', err.responseText);
            });
        },

        delete: function (datasetId, recordId, partial, name) {
            //first, get fields for dataset record based on selected language
            S.ajax.post('Datasets/DeleteRecord', { datasetId: datasetId, recordId: recordId }, (fieldslist) => {
                S.editor.datasets.records.show(datasetId, partial, name);
            }, (err) => {
                S.editor.error('', err.responseText);
            });
        },

        relationship: {
            list: {
                show: function (parentId, datasetId, recordId, column, keycolumn, lang, e) {
                    var target = $(e.target);
                    var container = target.parents('.vendor-input');
                    S.editor.fields.custom.list.tab('list-items', e);
                    var div = container.find('.list-items');
                    if (div.css('display') != 'none') {
                        //list is toggled on
                        var data = { parentId: parentId, datasetId: datasetId, recordId: recordId, column: column, keycolumn: keycolumn, lang: lang };
                        S.ajax.post('DataSets/RenderContentFieldListItems', data, (response) => {
                            var list = container.find('.list-items .contents');
                            list.html(response);
                            list.find('li').removeAttr('draggable');
                            list.find('.close-btn').remove(); //remove ability to delete list items
                        });
                    }
                }
            }
        }
    },

    viewOwner: function (e, userId) {
        e.preventDefault();
        e.cancelBubble = true;
        S.editor.users.details.show(userId);
        return false;
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
        //load datasets list into top menu
        S.editor.datasets.menu.load();
    }

    //add icons to the editor
    S.svg.load('/editor/vendors/datasets/icons.svg');
});