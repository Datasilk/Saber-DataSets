var gulp = require('gulp'),
    sevenBin = require('7zip-bin'),
    sevenZip = require('node-7z');

var app = 'DataSets';
var release = 'bin/Release/net6.0/';
var publish = 'bin/Publish/';

function publishToPlatform() {
    gulp.src([
        //include views
        'Sql/install.sql', 'Sql/uninstall.sql'
    ]).pipe(gulp.dest(publish + '/' + app + '/Sql', { overwrite: true }));

    gulp.src([
        //include views
        'Views/column-field.html', 'Views/columns.html', 'Views/create.html', 
        'Views/dataset.html', 'Views/datasource-filter.html', 'Views/record-menu.html',
        'Views/relationship.html', 'Views/update.html'
    ]).pipe(gulp.dest(publish + '/' + app + '/Views', { overwrite: true }));

    return gulp.src([
        //include custom resources
        'editor.js', 'editor.less', 'icons.svg', 'LICENSE', 'README.md',
        //include all files from published folder
        release + '/publish/*',
        //exclude unwanted dependencies
        '!' + release + '/publish/Core.dll',
        '!' + release + '/publish/Dapper.dll',
        '!' + release + '/publish/DOM.dll',
        '!' + release + '/publish/Saber.Core.dll',
        '!' + release + '/publish/Saber.Vendor.dll',
        '!' + release + '/publish/*.deps.json',
        '!' + release + '/publish/Azure.*.dll',
        '!' + release + '/publish/Microsoft.*.dll',
        '!' + release + '/publish/Newtonsoft.*.dll',
        '!' + release + '/publish/System.*.dll',
        '!' + release + '/publish/sni.dll'
    ]).pipe(gulp.dest(publish + '/' + app, { overwrite: true }));
}

gulp.task('publish:x64', () => {
    return publishToPlatform();
});

gulp.task('zip', () => {
    setTimeout(() => {
        //wait 500ms before creating zip to ensure no files are locked
        process.chdir(publish);
        sevenZip.add(app + '.7z', app, {
            $bin: sevenBin.path7za,
            recursive: true
        });
        process.chdir('../..');
    }, 500);
    return gulp.src('.');
});

gulp.task('publish', gulp.series('publish:x64', 'zip'));