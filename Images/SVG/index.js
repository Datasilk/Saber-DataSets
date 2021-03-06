var cheerio = require('cheerio')
var path = require('path')
var through2 = require('through2')
var gutil = require('gulp-util')

module.exports = function (config) {

  config = config || {}

  var isEmpty = true
  var fileName = config.filename ? config.filename : '';
  var ids = {}

  var resultSvg = '<svg xmlns="http://www.w3.org/2000/svg" ><defs/></svg>'

  var $ = cheerio.load(resultSvg, { xmlMode: true })
  var $combinedSvg = $('svg')
  var $combinedDefs = $('defs')

  return through2.obj(

    function transform (file, encoding, cb) {

      if (file.isStream()) {
        return cb(new gutil.PluginError('gulp-svgstore', 'Streams are not supported!'))
      }

      if (!file.cheerio) {
        file.cheerio = cheerio.load(file.contents.toString(), { xmlMode: true })
      }

      var $svg = file.cheerio('svg')
      var idAttr = path.basename(file.relative, path.extname(file.relative))
      var viewBoxAttr = $svg.attr('viewBox')
      var $symbol = $('<symbol/>')

      if (idAttr in ids) {
        return cb(new gutil.PluginError('gulp-svgstore', 'File name should be unique: ' + idAttr))
      }

      ids[idAttr] = true

      if (!fileName) {
        fileName = path.basename(file.base)
        if (fileName === '.' || !fileName) {
          fileName = 'svgstore.svg'
        } else {
          fileName = fileName.split(path.sep).shift() + '.svg'
        }
      }

      if (file && isEmpty) {
        isEmpty = false
      }

      $symbol.attr('id', idAttr)
      if (viewBoxAttr) {
        $symbol.attr('viewBox', viewBoxAttr)
      }

      var $defs = file.cheerio('defs')
      if ($defs.length > 0) {
        $combinedDefs.append($defs.contents())
        $defs.remove()
      }

      $symbol.append($svg.contents())
      $combinedSvg.append($symbol)
      cb()
    }

  , function flush (cb) {
      if (isEmpty) return cb()
      if ($combinedDefs.contents().length === 0) {
        $combinedDefs.remove()
      }
      var file = new gutil.File({ path: fileName, contents: new Buffer($.xml()) })
      file.cheerio = $
      this.push(file)
      cb()
    }
  )
}
