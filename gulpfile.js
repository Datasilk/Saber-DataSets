var gulp = require('gulp'),
    sevenBin = require('7zip-bin'),
    sevenZip = require('node-7z'),
    less = require('gulp-less');

var app = 'DataSets';
var release = 'bin/Release/net5.0/';
var publish = 'bin/Publish/';

function publishToPlatform(platform) {
    return gulp.src([
        //include custom resources
        'column-field.html', 'columns.html', 'create.html', 'dataset.html', 'datasource-filter.html',
        'editor.js', 'editor.less', 'icons.svg', 'record-menu.html', 'relationship.html', 'update.html',
        //include all files from published folder
        release + platform + '/publish/*',
        //exclude unwanted dependencies
        '!' + release + platform + '/publish/Core.dll',
        '!' + release + platform + '/publish/Saber.Core.dll',
        '!' + release + platform + '/publish/Saber.Vendor.dll',
        '!' + release + platform + '/publish/*.deps.json'
    ]).pipe(gulp.dest(publish + '/' + platform + '/' + app, { overwrite: true }));
}

gulp.task('publish:win-x64', () => {
    return publishToPlatform('win-x64');
});

gulp.task('publish:linux-x64', () => {
    return publishToPlatform('linux-x64');
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

gulp.task('publish', gulp.series('publish:win-x64', 'publish:linux-x64', 'zip'));