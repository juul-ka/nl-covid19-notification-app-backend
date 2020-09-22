const fs = require("fs");
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;
const tmp = require('tmp');
const extraConsoleLogging = true; // switch to true to log debug stuff

let testsSig = function (payload, confirmationKey) {
    return new Promise(function (resolve) {

        let tmpObj = tmp.fileSync({mode: 0644, prefix: 'tempfile', postfix: '.txt'});
        // let path = tmpObj.name.substring(0, tmpObj.name.lastIndexOf('/'));
        // let file = tmpObj.name.substring(tmpObj.name.lastIndexOf('/') + 1);

        let optionsA = {
            encoding: 'utf8'
        };
        let optionsB = {
            encoding: 'binary'
        };
        fs.writeFileSync(tmpObj.name, payload)

        let keyCommand = `echo ${confirmationKey} | base64 -d | xxd -p -c 256`;
        let KEY = execSync(keyCommand,optionsA)
        let sigCommand = `cat ${tmpObj.name} | openssl sha256 -mac HMAC -macopt hexkey:${KEY}`
        const sig = execSync(sigCommand, optionsB);
        let resultHex = sig.replace("(stdin)= ", "");
        let resultBase64 = Buffer.from(resultHex, 'hex').toString('base64');
        let resultBase64UrlEncode = encodeURIComponent(resultBase64);

        if (extraConsoleLogging) {
            console.log('result: ' + sig);
            console.log('resultHex: ' + resultHex);
            console.log('resultBase64: ' + resultBase64);
            console.log('resultBase64UrlEncode: ' + resultBase64UrlEncode);
        }

        resolve({sig: resultBase64UrlEncode});


        // const sig = execShellCommand(keyCommand,options).then(KEY => {
        //
        //     let sigCommand = `cd ${path} cat ${file} | openssl sha256 -mac HMAC -macopt hexkey:${KEY} -binary`;
        //     execShellCommand(sigCommand,options).then(result => {
        //         // remove prefix fromm the result
        //         var resultHex = result.replace("(stdin)= ", "");
        //         var resultBase64 = Buffer.from(resultHex, 'hex').toString('base64');
        //         var resultBase64UrlEncode = encodeURIComponent(resultBase64);
        //
        //         if(extraConsoleLogging) {
        //             console.log('result: ' + result);
        //             console.log('resultHex: ' + resultHex);
        //             console.log('resultBase64: ' + resultBase64);
        //             console.log('resultBase64UrlEncode: ' + resultBase64UrlEncode);
        //         }
        //         // tmpObj.removeCallback(); // remove temp file
        //         resolve({sig:resultBase64UrlEncode});
        //     });
        //     });
        //
        // function execShellCommand(cmd) {
        //     return new Promise((resolve, reject) => {
        //         exec(cmd, options,(error, stdout, stderr) => {
        //             if (error) {
        //                 console.warn("!!! ERROR !!!");
        //                 console.warn(error);
        //             }
        //             resolve(stdout? stdout : stderr);
        //         })
        //     })
        // };
    });
}
module.exports = testsSig;